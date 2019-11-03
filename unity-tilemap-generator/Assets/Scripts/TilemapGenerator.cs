using UnityEngine;
// Added
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public enum RiverMethod
{
    Path,
    Height
}

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
    private int NumRivers = 0;

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
    private string Seed = "";

    [Header("Development")]
    public bool RegenerateLoop = false;
    public RiverMethod RiverMethod = RiverMethod.Height;

    // Private members
    private TerrainGenerator terrainGenerator;
    private WaterGenerator waterGenerator;
    private static readonly float colorIncrement = 0.1f;
    private Dictionary<RiverMethod, System.Action<WaterGenerator, TerrainGenerator, int, int, int>> RiverMethods = new Dictionary<RiverMethod, System.Action<WaterGenerator, TerrainGenerator, int, int, int>>()
    {
        { RiverMethod.Path, (waterGenerator, terrainGenerator, snowLevel, waterLevel, numRivers) => {
                waterGenerator.RiversByPath(terrainGenerator, snowLevel, waterLevel, numRivers);
            }
        },
        { RiverMethod.Height, (waterGenerator, terrainGenerator, snowLevel, waterLevel, numRivers) => {
                waterGenerator.RiversByHeight(terrainGenerator, snowLevel, waterLevel, numRivers);
            }
        },
    };

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(Seed.GetHashCode());
        terrainGenerator = new TerrainGenerator(Width, Length);
        waterGenerator = new WaterGenerator(Width, Length);
        Refresh();
    }

    // Update is called once per frame
    // Only used for debugging
    void Update()
    {
        if (RegenerateLoop) Regenerate();
        else if (Input.GetKeyUp(KeyCode.Space)) Regenerate();
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = WaterMap.WorldToCell(mouseWorldPos);
            coordinate.z = -999;
            foreach (var a in waterGenerator.Graph)
            {
                if (a.Item.x == coordinate.x && a.Item.y == coordinate.y && a.Item.z > coordinate.z)
                {
                    coordinate.z = a.Item.z;
                }
            }
            Debug.Log("Clicked <color=blue><b>water</b></color> at <b>" + coordinate + "</b>");
        }
        if (Input.GetMouseButtonUp(1))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = TerrainMap.WorldToCell(mouseWorldPos);
            coordinate.z = terrainGenerator.HeightMap[coordinate.x, coordinate.y];
            Debug.Log("Clicked <color=green><b>terrain</b></color> at <b>" + coordinate + "</b>");
        }
    }

    /// <summary>
    /// Refreshes map with current noise
    /// </summary>
    [ContextMenu("Refresh")]
    public void Refresh()
    {
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
        waterGenerator.Reset();
        waterGenerator.Fill(terrainGenerator, WaterLevel);
        RiverMethods[RiverMethod](waterGenerator, terrainGenerator, SnowLevel, WaterLevel, NumRivers);
        Debug.Log("<color=blue><b>Water</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b>");
        System.GC.Collect();
        return;
    }

    private void draw()
    {
        // Clear
        TerrainMap.ClearAllTiles();
        WaterMap.ClearAllTiles();

        // Terrain
        var nodes = terrainGenerator.Graph.GetEnumerator();
        TileBase tile;
        while (nodes.MoveNext())
        {
            if (nodes.Current.Item.z >= SnowLevel) tile = SnowTile;
            else tile = FloorTile;
            setTile(TerrainMap, nodes.Current.Item, tile);
        }

        // Water
        nodes = waterGenerator.Graph.GetEnumerator();
        while (nodes.MoveNext()) setTile(WaterMap, nodes.Current.Item, WaterTile);
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
