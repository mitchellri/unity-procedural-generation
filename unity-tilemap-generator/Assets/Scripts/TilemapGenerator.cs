using UnityEngine;
// Added
using UnityEngine.Tilemaps;

public class TilemapGenerator : MonoBehaviour
{
    // Inspector parameters
    [Header("Assets")]
    public Tilemap WorldMap;
    public Tile FloorTile;
    public Tile WaterTile;
    public Tile SnowTile;

    [Header("Map")]
    public int Width;
    public int Length;
    public int Height;
    public float WaterLevel;
    public float SnowLevel;
    public bool Radial = false;

    [Header("Noise")]
    [Tooltip("Smoothness of terrain.")]
    public float InverseFrequency = 175;
    [Tooltip("Edge smoothness.")]
    public int Octaves = 8;
    [Range(0, 3)]
    [Tooltip("Noise randomness.")]
    public float Lacunarity = 2;
    [Range(0, 3)]
    [Tooltip("Length contrast.")]
    public float Amplitude = 1;
    [Range(0, 1)]
    [Tooltip("Octave exponential for amplitude")]
    public float Gain = 0.5f;
    [Tooltip("Noise multiplier")]
    public float Scale = 10;
    public int PeriodX = 0;
    public int PeriodY = 0;
    public int PeriodZ = 0;
    [Tooltip("Cannot be changed after start.")]
    public string Seed = "";

    [Header("Erosion")]
    public bool DropletErosion = true;
    public bool NaturalRivers = true;
    [Range(0, 1)]
    [Tooltip("Inertia of flowing water.")]
    public float DirectionInertia = .1f;
    [Range(0, 1)]
    [Tooltip("Rate of sediment deposition.")]
    public float SedimentDeposit = .1f;
    [Range(0, 1)]
    [Tooltip("Used with sediment carry capacity.")]
    public float MinSlope = .1f;
    [Tooltip("Sediment carry capacity.")]
    public float SedimentCapacity = 10;
    [Range(0, 1)]
    [Tooltip("Rate of sediment deposition.")]
    public float DepositionSpeed = .02f;
    [Range(0, 1)]
    [Tooltip("Rate of terrain erosion.")]
    public float ErosionSpeed = .9f;
    [Range(0, 0.1f)]
    [Tooltip("Rate of water evaporation.")]
    public float EvaporationSpeed = .001f;

    [Header("Development")]
    public bool RegenerateLoop = false;
    public bool ShowWetness = false;
    public int Z;

    // Private members
    private TerrainGenerator terrainGenerator;
    private WaterGenerator waterGenerator;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(Seed.GetHashCode());
        terrainGenerator = new TerrainGenerator(Width, Length, Height);
        waterGenerator = new WaterGenerator(Width, Length, Height);
        Refresh();
    }

    // Update is called once per frame
    // Only used for debugging
    void Update()
    {
        if (RegenerateLoop) Regenerate();
        else if (Input.GetKeyUp(KeyCode.Space)) Regenerate();
        if (Input.GetKey(KeyCode.Equals))
        {
            Z += 1;
            Refresh();
        }
        else if (Input.GetKey(KeyCode.Minus))
        {
            Z -= 1;
            Refresh();
        }
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 coordinate = WorldMap.WorldToCell(mouseWorldPos);
            coordinate.z = waterGenerator.HeightMap[(int)coordinate.x, (int)coordinate.y];
            Debug.Log("Clicked <color=blue><b>water</b></color> at <b>" + coordinate + "</b>");
        }
        if (Input.GetMouseButtonUp(1))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 coordinate = WorldMap.WorldToCell(mouseWorldPos);
            coordinate.z = terrainGenerator.HeightMap[(int)coordinate.x, (int)coordinate.y];
            Debug.Log("Clicked <color=green><b>terrain</b></color> at <b>" + coordinate + "</b> with <color=blue>wetness</color> " + (terrainGenerator.WetnessMap[(int)coordinate.x, (int)coordinate.y] * 100 / terrainGenerator.AbsorptionCapacity) + "%");
        }
    }

    /// <summary>
    /// Refreshes map with current noise
    /// </summary>
    [ContextMenu("Refresh")]
    public void Refresh()
    {
        var time = Time.realtimeSinceStartup;
        generate();
        Debug.Log("<b>Map</b> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b>");
        time = Time.realtimeSinceStartup;
        draw();
        Debug.Log("<b>Map</b> drawn in <b>" + (Time.realtimeSinceStartup - time) + "</b>");
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
        terrainGenerator.GenerateTerrain(InverseFrequency, Lacunarity, Gain, Amplitude, Octaves, Scale, PeriodX, PeriodY, PeriodZ, Z);
        if (Radial) terrainGenerator.Radial(Length / 4, Length / 2, Width / 2, Length / 2);
        if (DropletErosion) terrainGenerator.DropletErosion(DirectionInertia, SedimentDeposit, MinSlope, SedimentCapacity, DepositionSpeed, ErosionSpeed, EvaporationSpeed);
        Debug.Log("<color=green><b>Terrain</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b> from " + terrainGenerator.MinHeight + " to " + terrainGenerator.MaxHeight);

        // Generate rivers
        time = Time.realtimeSinceStartup;
        waterGenerator.Reset();
        waterGenerator.Fill(terrainGenerator, WaterLevel);
        if (NaturalRivers)
            waterGenerator.FillExcessWetness(terrainGenerator, WaterLevel, DirectionInertia, SedimentDeposit, MinSlope, SedimentCapacity, DepositionSpeed, ErosionSpeed, EvaporationSpeed);
        Debug.Log("<color=blue><b>Water</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b>");
        System.GC.Collect();
        return;
    }

    private void draw()
    {
        // Clear
        WorldMap.ClearAllTiles();

        Vector3 vector = new Vector3();
        Tile tile;
        for (int x = 0; x < Width; ++x)
        {
            vector.x = x;
            for (int y = 0; y < Length; ++y)
            {
                vector.y = y;
                vector.z = terrainGenerator.HeightMap[x, y];

                if (vector.z >= SnowLevel) tile = SnowTile;
                else tile = FloorTile;

                setTile(WorldMap, vector, tile);

                if (waterGenerator.HeightMap[x, y] > float.MinValue)
                {
                    vector.z = waterGenerator.HeightMap[x, y];
                    setTile(WorldMap, vector, WaterTile);
                }
            }
        }
    }

    private void setTile(Tilemap tileMap, Vector3 vector, Tile tile, float? colorZ = null)
    {
        float brightness;
        float wetness;
        if (ShowWetness)
        {
            wetness = terrainGenerator.WetnessMap[(int)vector.x, (int)vector.y];
            wetness *= 5;
        }
        else wetness = 0;
        // Adjust color
        if (tile == FloorTile)
        {
            brightness = 1 - (terrainGenerator.MaxHeight - vector.z) / (terrainGenerator.MaxHeight - terrainGenerator.MinHeight);
            tile.color = Color.HSVToRGB(0.25f, 0.8f, brightness - wetness);
        }
        else if (tile == WaterTile)
        {
            if (waterGenerator.MaxHeight == waterGenerator.MinHeight) brightness = 1.5f - (terrainGenerator.MaxHeight - vector.z) / (terrainGenerator.MaxHeight - WaterLevel);
            else brightness = 1.5f - (waterGenerator.MaxHeight - vector.z) / (waterGenerator.MaxHeight - Mathf.Max(waterGenerator.MinHeight, terrainGenerator.MinHeight));
            Color color = Color.HSVToRGB(0.55f, 0.75f, brightness);
            color.a = 0.8f;
            tile.color = color;
        }
        else if (tile == SnowTile)
        {
            brightness = 1 - (terrainGenerator.MaxHeight - vector.z) / (terrainGenerator.MaxHeight - terrainGenerator.MinHeight);
            tile.color = Color.HSVToRGB(0.6f, 0.1f, brightness - wetness);
        }
        tileMap.SetTile(new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z), tile);
    }
}
