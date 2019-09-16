using UnityEngine;
// Added
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TilemapGenerator : MonoBehaviour
{
    // Parameters
    public Tilemap Floor;
    public Tilemap RiverMap;
    public TileBase FloorTile;
    public TileBase WaterTile;
    public TileBase SnowTile;
    public int Width;
    public int Length;
    public int MinimumSmoothness;
    public int MaximumSmoothness;
    public int Octaves = 8;
    public float Lacunarity = 2;
    public float Amplitude = 1;
    public int WaterLevel;
    public int SnowLevel;
    public int MaxRivers;
    // Private members
    private TerrainGenerator terrainGenerator;
    private RiverGenerator riverGenerator;
    private static readonly float colorIncrement = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        terrainGenerator = new TerrainGenerator(Width, Length);
        riverGenerator = new RiverGenerator(Width, Length);
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space)) Reset();
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = RiverMap.WorldToCell(mouseWorldPos);
            foreach (var a in riverGenerator.Graph)
            {
                if (a.Item.x == coordinate.x && a.Item.y == coordinate.y)
                {
                    coordinate.z = a.Item.z;
                    break;
                }
            }
            Debug.Log("Clicked <color=blue><b>river</b></color> at <b>" + coordinate + "</b>");
        }
        if (Input.GetMouseButtonUp(1))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = Floor.WorldToCell(mouseWorldPos);
            coordinate.z = terrainGenerator.HeightMap[coordinate.x, coordinate.y];
            Debug.Log("Clicked <color=green><b>terrain</b></color> at <b>" + coordinate + "</b>");
        }
    }

    public void Reset()
    {
        Debug.ClearDeveloperConsole();
        Generate();
        Redraw();
    }

    public void Redraw()
    {
        Floor.ClearAllTiles();
        RiverMap.ClearAllTiles();
        Draw();
    }

    public void Generate()
    {
        // Generate terrain
        var time = Time.realtimeSinceStartup;
        terrainGenerator.GenerateTerrain(MinimumSmoothness, MaximumSmoothness, Lacunarity, Amplitude, Octaves);
        Debug.Log("<color=green><b>Terrain</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b>");

        // Generate rivers
        time = Time.realtimeSinceStartup;
        riverGenerator.Reset();
        // Find travel nodes
        List<uint> sourceList = new List<uint>(),
            destinationList = new List<uint>();
        var nodes = terrainGenerator.Graph.GetEnumerator();
        while (nodes.MoveNext())
        {
            if (nodes.Current.Item.z == WaterLevel)
            {
                foreach (uint pId in terrainGenerator.Graph.Parents(nodes.Current.Key))
                {
                    if (terrainGenerator.Graph[pId].Item.z == WaterLevel + 1)
                    {
                        destinationList.Add(nodes.Current.Key);
                        break;
                    }
                }
            }
            // Any snow
            else if (nodes.Current.Item.z >= SnowLevel) sourceList.Add(nodes.Current.Key);
            // Edges of snow
            // No edges moving upward, so no parents in lower z-axis
            /*else if (nodes.Current.Item.z == SnowLevel - 1)
            {
                foreach (uint pId in terrainGenerator.Graph.Parents(nodes.Current.Key))
                {
                    // One parent can have several children
                    if (terrainGenerator.Graph[pId].Item.z == SnowLevel && !sourceList.Contains(pId))
                    {
                        sourceList.Add(nodes.Current.Key);
                        break;
                    }
                }
            }*/
        }
        riverGenerator.GenerateRivers(terrainGenerator, sourceList, destinationList, MaxRivers);
        Debug.Log("<color=blue><b>Rivers</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b>");
        return;
    }

    public void Draw()
    {
        var nodes = terrainGenerator.Graph.GetEnumerator();
        while (nodes.MoveNext()) setTile(Floor, nodes.Current.Item);
        nodes = riverGenerator.Graph.GetEnumerator();
        Vector3Int currentVector;
        while (nodes.MoveNext())
        {
            currentVector = nodes.Current.Item;
            currentVector.z = riverGenerator.HeightMap[currentVector.x, currentVector.y];
            setTile(RiverMap, currentVector, WaterTile);
            // Rivermap adjusts terrain graph to account for rivers
            // If the the generated terrain generated water in the place of the river, replace it with floor
            --currentVector.z;
            if (currentVector.z <= WaterLevel && Floor.HasTile(currentVector)) setTile(Floor, currentVector, FloorTile);
        }
    }

    private void setTile(Tilemap tileMap, Vector3Int vector, TileBase tile = null, bool colorTile = true)
    {
        int x = vector.x,
            y = vector.y,
            z = vector.z;

        // Dynamic tile choice
        if (tile == null)
        {
            if (z <= WaterLevel) tile = WaterTile;
            else if (z >= SnowLevel) tile = SnowTile;
            else tile = FloorTile;
        }

        // Place tile
        tileMap.SetTile(vector, tile);
        tileMap.SetTileFlags(vector, TileFlags.None);

        // Adjust color
        if (colorTile)
        {
            Color color = tileMap.GetColor(vector);
            if (tile == SnowTile)
            {
                color.r -= colorIncrement * (z - SnowLevel);
                color.g -= colorIncrement * (z - SnowLevel);
            }
            else if (tile == WaterTile) color.g += colorIncrement * (z - WaterLevel);
            else color.g += colorIncrement * z;
            tileMap.SetColor(vector, color);
        }
    }
}
