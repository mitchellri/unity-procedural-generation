using UnityEngine;
// Added
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TilemapGenerator : MonoBehaviour
{
    // Inspector parameters
    [Header("Assets")]
    public Tilemap TerrainMap;
    public Tilemap WaterMap;
    public TileBase FloorTile;
    public TileBase WaterTile;
    public TileBase SnowTile;

    [Header("Map")]
    public int Width;
    public int Length;
    public int WaterLevel;
    public int SnowLevel;
    [SerializeField]
    private int MaxRivers = 0;

    [Header("Noise")]
    [SerializeField]
    [Tooltip("Smoothness of terrain.")]
    private int Smoothness = 175;
    [SerializeField]
    [Tooltip("Edge smoothness.")]
    private int Octaves = 8;
    [SerializeField]
    [Range(0, 3)]
    [Tooltip("Noise randomness.")]
    private float Lacunarity = 2;
    [SerializeField]
    [Range(0, 3)]
    [Tooltip("Length contrast.")]
    private float Amplitude = 1;
    [SerializeField]
    [Tooltip("Cannot be changed after start.")]
    private string Seed;

    // Private members
    private TerrainGenerator terrainGenerator;
    private RiverGenerator riverGenerator;
    private static readonly float colorIncrement = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(Seed.GetHashCode());
        terrainGenerator = new TerrainGenerator(Width, Length);
        riverGenerator = new RiverGenerator(Width, Length);
        Refresh();
    }

    // Update is called once per frame
    // Only used for debugging
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = WaterMap.WorldToCell(mouseWorldPos);
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
            Vector3Int coordinate = TerrainMap.WorldToCell(mouseWorldPos);
            coordinate.z = terrainGenerator.HeightMap[coordinate.x, coordinate.y];
            Debug.Log("Clicked <color=green><b>terrain</b></color> at <b>" + coordinate + "</b>");
        }
        /*if (Input.GetKeyUp(KeyCode.R))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = TerrainMap.WorldToCell(mouseWorldPos);

            riverGenerator.GenerateRiverByHeight(terrainGenerator, (uint)(coordinate.x + coordinate.y * terrainGenerator.Width), WaterLevel - 1);
            terrainGenerator.SetGraph(terrainGenerator.HeightMap);
            draw();
        }*/
    }

    /// <summary>
    /// Refreshes map with current noise
    /// </summary>
    [ContextMenu("Refresh")]
    public void Refresh()
    {
        terrainGenerator.SetSize(Width, Length);
        riverGenerator.SetSize(Width, Length);
        generate();
        draw();
    }

    /// <summary>
    /// Refreshes map with new noise
    /// </summary>
    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        terrainGenerator.Reset();
        Refresh();
    }

    private void generate()
    {
        // Generate terrain
        var time = Time.realtimeSinceStartup;
        terrainGenerator.GenerateTerrain(Smoothness, Lacunarity, Amplitude, Octaves);
        Debug.Log("<color=green><b>Terrain</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b>");

        // Generate rivers
        time = Time.realtimeSinceStartup;
        riverGenerator.GenerateRiversByHeight(terrainGenerator, SnowLevel, WaterLevel - 1, MaxRivers);
        Debug.Log("<color=blue><b>Rivers</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b>");
        return;
    }

    private void draw()
    {
        // Clear
        TerrainMap.ClearAllTiles();
        WaterMap.ClearAllTiles();

        // Terrain
        var nodes = terrainGenerator.Graph.GetEnumerator();
        Vector3Int index;
        TileBase tile;
        while (nodes.MoveNext())
        {
            if (nodes.Current.Item.z >= SnowLevel) tile = SnowTile;
            else tile = FloorTile;
            setTile(TerrainMap, nodes.Current.Item, tile);
            if (nodes.Current.Item.z < WaterLevel)
            {
                index = nodes.Current.Item;
                ++index.z;
                for (; index.z <= WaterLevel; ++index.z)
                    setTile(WaterMap, index, WaterTile, nodes.Current.Item.z - index.z + 1);
            }
        }

        // Water
        nodes = riverGenerator.Graph.GetEnumerator();
        Vector3Int currentVector;
        while (nodes.MoveNext())
        {
            currentVector = nodes.Current.Item;
            currentVector.z = riverGenerator.HeightMap[currentVector.x, currentVector.y];
            setTile(WaterMap, currentVector, WaterTile);
        }
    }

    private void setTile(Tilemap tileMap, Vector3Int vector, TileBase tile, int? colorZ = null)
    {
        int x = vector.x,
            y = vector.y,
            z = colorZ is null ? vector.z : (int)colorZ;

        // Place tile
        tileMap.SetTile(vector, tile);
        tileMap.SetTileFlags(vector, TileFlags.None);

        // Adjust color
        Color color = tileMap.GetColor(vector);
        if (tile == FloorTile) color.g += colorIncrement * z;
        else if (tile == WaterTile) color.g += colorIncrement * (z - WaterLevel);
        else if (tile == SnowTile)
        {
            color.r -= colorIncrement * (z - SnowLevel);
            color.g -= colorIncrement * (z - SnowLevel);
        }
        tileMap.SetColor(vector, color);
    }
}
