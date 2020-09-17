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
    public int SkyHeight;
    public int UndergroundHeight;
    public float WaterLevel;
    public float SnowLevel;
    public bool Flood = true;
    public bool Radial = false;
    [Range(0, 1)]
    public float RadiusCenterX = 0.5f;
    [Range(0, 1)]
    public float RadiusCenterY = 0.5f;
    [Range(0, 1)]
    public float RadiusInner = 0.25f;
    [Range(0, 1)]
    public float RadiusOuter = 0.5f;

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
    public bool Seamless = false;
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

    [Header("Caves")]
    public int CaveCount = 0;
    public int CaveSegmentLength = 50;
    public float CaveTwistiness = 10;
    public int CaveRadius = 3;
    public float CaveRadiusVarianceScale = 0;

    [Header("Development")]
    public bool RegenerateLoop = false;
    public bool ShowWetness = false;

    // Private members
    private int Z = -1;
    private TerrainGenerator terrainGenerator;
    private WaterGenerator waterGenerator;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(Seed.GetHashCode());
        terrainGenerator = new TerrainGenerator();
        waterGenerator = new WaterGenerator(terrainGenerator);
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
            if (Z + 1 < terrainGenerator.Height)
            {
                Z += 1;
                draw();
            }
        }
        else if (Input.GetKey(KeyCode.Minus))
        {
            if (Z - 1 >= terrainGenerator.MinHeight)
            {
                Z -= 1;
                draw();
            }
        }
        /*if (Input.GetKey(KeyCode.Q))
        {
            CaveSegmentLength -= 1;
            Refresh();
        }
        else if (Input.GetKey(KeyCode.E))
        {
            CaveSegmentLength += 1;
            Refresh();
        }*/
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 coordinate = WorldMap.WorldToCell(mouseWorldPos);
            coordinate.z = Z;
            coordinate.z = waterGenerator.GetFloorAt(coordinate);
            coordinate.z += terrainGenerator.WorldMap[(int)coordinate.x, (int)coordinate.y, (int)coordinate.z];
            Debug.Log("Clicked <color=blue><b>water</b></color> at <b>" + coordinate + "</b>");
        }
        if (Input.GetMouseButtonUp(1))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 coordinate = WorldMap.WorldToCell(mouseWorldPos);
            coordinate.z = Z;
            coordinate.z = terrainGenerator.GetFloorAt(coordinate);
            if (coordinate.z == terrainGenerator.Height) coordinate.z = terrainGenerator.Height - 1;
            Debug.Log("Clicked <color=green><b>terrain</b></color> at <b>" + coordinate
                + "</b> with <color=blue>wetness</color> " + (terrainGenerator.WetnessMap[(int)coordinate.x, (int)coordinate.y, (int)coordinate.z] * 100 / terrainGenerator.AbsorptionCapacity) + "%"
                + "and <color=green><b>fill percent</b></color> of " + (terrainGenerator.WorldMap[(int)coordinate.x, (int)coordinate.y, (int)coordinate.z] * 100) + "%");
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
        Debug.Log("Generating new <b>map</b>");

        // Generate terrain
        var time = Time.realtimeSinceStartup;
        terrainGenerator.Width = Width;
        terrainGenerator.Length = Length;
        terrainGenerator.SkyHeight = SkyHeight;
        terrainGenerator.UndergroundHeight = UndergroundHeight;
        terrainGenerator.InverseFrequency = InverseFrequency;
        terrainGenerator.Lacunarity = Lacunarity;
        terrainGenerator.Gain = Gain;
        terrainGenerator.Amplitude = Amplitude;
        terrainGenerator.Octaves = Octaves;
        terrainGenerator.Scale = Scale;
        terrainGenerator.Period.Set(PeriodX, PeriodY, PeriodZ);
        terrainGenerator.RadiusInner = Length * RadiusInner;
        terrainGenerator.RadiusOuter = Length * RadiusOuter;
        terrainGenerator.RadiusCenter.Set(Mathf.RoundToInt(Width * RadiusCenterX), Mathf.RoundToInt(Length * RadiusCenterX));
        terrainGenerator.GenerateTerrain(Seamless, Radial);
        Z = terrainGenerator.Height - 1;

        // Generate caves
        terrainGenerator.CaveSegmentLength = CaveSegmentLength;
        terrainGenerator.CaveTwistiness = CaveTwistiness;
        terrainGenerator.CaveRadius = CaveRadius;
        terrainGenerator.CaveRadiusVarianceScale = CaveRadiusVarianceScale;
        // Pos variables
        int x, y;
        for (int curCave = 0; curCave < CaveCount; ++curCave)
        {
            x = Random.Range(0, Width - 1);
            y = Random.Range(0, Length - 1);
            terrainGenerator.GenerateCave(x, y, (int)terrainGenerator.GetFloorAt(new Vector3(x, y, 0)));
        }

        // Erode terrain
        terrainGenerator.DirectionInertia = DirectionInertia;
        terrainGenerator.SedimentDeposit = SedimentDeposit;
        terrainGenerator.MinSlope = MinSlope;
        terrainGenerator.SedimentCapacity = SedimentCapacity;
        terrainGenerator.DepositionSpeed = DepositionSpeed;
        terrainGenerator.ErosionSpeed = ErosionSpeed;
        terrainGenerator.EvaporationSpeed = EvaporationSpeed;
        if (DropletErosion) terrainGenerator.DropletErosion();
        Debug.Log("<color=green><b>Terrain</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b> from " + terrainGenerator.MinHeight + " to " + terrainGenerator.MaxHeight);

        // Generate rivers
        time = Time.realtimeSinceStartup;
        waterGenerator.WaterLevel = WaterLevel;

        waterGenerator.Reset();
        if (Flood)
        {
            waterGenerator.Fill();
        }
        if (NaturalRivers)
        {
            waterGenerator.FillExcessWetness();
        }
        Debug.Log("<color=blue><b>Water</b></color> generated in <b>" + (Time.realtimeSinceStartup - time) + "</b>");
        System.GC.Collect();
        return;
    }

    private void draw()
    {
        // Clear
        WorldMap.ClearAllTiles();

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        Vector3 vector = new Vector3();
        for (int x = 0; x < Width; ++x)
        {
            vector.x = x;
            for (int y = 0; y < Length; ++y)
            {
                vector.y = y;
                vector.z = terrainGenerator.Height - 1;
                vector.z = terrainGenerator.GetFloorAt(vector);
                if (vector.z > maxHeight)
                {
                    maxHeight = vector.z;
                }
                if (vector.z < minHeight)
                {
                    minHeight = vector.z;
                }
            }
        }

        Tile tile;
        Vector3 terrainVector = new Vector3();
        for (int x = 0; x < Width; ++x)
        {
            vector.x = x;
            for (int y = 0; y < Length; ++y)
            {
                vector.y = y;
                if (terrainGenerator.WorldMap != null)
                {
                    if (Z >= 0)
                    {
                        vector.z = Z;
                    }
                    else
                    {
                        vector.z = terrainGenerator.Height - 1;
                    }
                    vector.z = terrainGenerator.GetFloorAt(vector);

                    if (vector.z >= SnowLevel) tile = SnowTile;
                    else tile = FloorTile;

                    setTile(WorldMap, vector, tile, null, minHeight, maxHeight);
                    terrainVector.Set(vector.x, vector.y, vector.z);

                    if (Z >= 0)
                    {
                        vector.z = Z;
                    }
                    else
                    {
                        vector.z = waterGenerator.Height - 1;
                    }
                    vector.z = waterGenerator.GetFloorAt(vector);
                    if ((vector.x == 31) && (vector.y == 22))
                    {
                        float tempasdf = 2;
                    }
                    if (vector.z > 0 && (vector.z >= (int)terrainVector.z))
                    {
                        vector.z += terrainGenerator.WorldMap[(int)terrainVector.x, (int)terrainVector.y, (int)terrainVector.z]; // Place above land
                        setTile(WorldMap, vector, WaterTile, null, minHeight, maxHeight);
                    }
                }
            }
        }
    }

    private void setTile(Tilemap tileMap, Vector3 vector, Tile tile, float? colorZ, float minHeight, float maxHeight)
    {
        float brightness;
        float wetness;
        Vector3Int vectorInt = new Vector3Int(
                (int)vector.x,
                (int)vector.y,
                (int)vector.z
            );
        if (ShowWetness && tile != WaterTile)
        {
            wetness = terrainGenerator.WetnessMap[vectorInt.x, vectorInt.y, vectorInt.z];
            wetness *= 5;
        }
        else wetness = 0;
        // Adjust color
        if (tile == FloorTile)
        {
            brightness = 1 - (maxHeight - vector.z) / (maxHeight - minHeight);
            tile.color = Color.HSVToRGB(0.25f, 0.8f, brightness - wetness);
        }
        else if (tile == WaterTile)
        {
            if (maxHeight == minHeight) brightness = 1.5f - (maxHeight - vector.z) / (maxHeight - WaterLevel);
            else brightness = 1.5f - (maxHeight - vector.z) / (maxHeight - Mathf.Max(minHeight, minHeight));
            Color color = Color.HSVToRGB(0.55f, 0.75f, brightness);
            color.a = 0.8f;
            tile.color = color;
        }
        else if (tile == SnowTile)
        {
            brightness = 1 - (maxHeight - vector.z) / (maxHeight - minHeight);
            tile.color = Color.HSVToRGB(0.6f, 0.1f, brightness - wetness);
        }
        tileMap.SetTile(new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z), tile);
    }
}
