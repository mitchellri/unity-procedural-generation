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
        riverGenerator = new RiverGenerator();
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            Reset();
        }
    }

    public void Reset()
    {
        Debug.ClearDeveloperConsole();
        Floor.ClearAllTiles();
        RiverMap.ClearAllTiles();
        Generate();
        Draw();
    }

    public void Generate()
    {
        // Generate terrain
        terrainGenerator.GenerateTerrain(MinimumSmoothness,MaximumSmoothness,Lacunarity,Amplitude,Octaves);

        // Generate rivers
        // <Todo>
        // For each river destination, expand the lake if small
        // </Todo>
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
        riverGenerator.GenerateRivers(terrainGenerator.Graph, sourceList, destinationList, MaxRivers);
        return;
    }

    public void Draw()
    {
        var nodes = terrainGenerator.Graph.GetEnumerator();
        while (nodes.MoveNext()) setTile(Floor, nodes.Current.Item);
        nodes = riverGenerator.Graph.GetEnumerator();
        while (nodes.MoveNext()) setTile(RiverMap, nodes.Current.Item, WaterTile);
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
