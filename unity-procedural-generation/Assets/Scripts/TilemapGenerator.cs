using UnityEngine;
// Added
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Dijkstra.NET.Graph;
using Dijkstra.NET.ShortestPath;
using System.Linq;

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
    private PerlinNoise perlinNoise;
    private Graph<Vector3Int, int> pathFinding;
    private static readonly float colorIncrement = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        perlinNoise = new PerlinNoise(Width, Length);
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            perlinNoise.ResetGradientArray();
            Floor.ClearAllTiles();
            Generate();
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

    private uint[,] generateTerrain()
    {
        pathFinding = new Graph<Vector3Int, int>();
        uint[,] idGrid = new uint[Width, Length];
        Vector3Int min = Floor.cellBounds.min;
        int smoothness = Random.Range(MinimumSmoothness, MaximumSmoothness),
            z;
        Vector3Int tile = new Vector3Int(),
            index = new Vector3Int();
        Color color;
        for (int x = 0; x < Width; ++x)
        {
            tile.x = min.x + x;
            for (int y = 0; y < Length; ++y)
            {
                tile.y = min.y + y;
                z = (int)Mathf.Round(
                    /* Setting impact:
                     * Frequency: smoothness of terrain (1/smoothness)
                     * Lacunarity: Randomness (2)
                     * Amplitude: Length contrast (1)
                     * Octaves: Edge smoothness (8)
                     */
                    /* Potential parameters:
                     * Min Smooth:   90     150
                     * Max Smooth:   100    300
                     * Octaves:      8      5
                     * Lacunarity:   2      1.75
                     * Amplitude:    1      1
                     */
                    perlinNoise.FractionalBrownianMotion(
                        x, y,
                        frequency: (float)1 / smoothness,
                        lacunarity: Lacunarity,
                        amplitude: Amplitude,
                        octaves: Octaves
                    ) * 10);
                index.Set(x, y, z);
                idGrid[x, y] = pathFinding.AddNode(index);
                tile.z = min.z + z;
                setTile(Floor, index);
            }
        }
        createNetwork(idGrid);
        return idGrid;
    }
    private int movementCostFunction(Vector3Int movementVector)
    {
        return movementVector.z + Mathf.Abs(movementVector.y) + Mathf.Abs(movementVector.x);
    }

    private void removeParents(uint id)
    {
        foreach (var parent in pathFinding.Parents(id))
        {
            // pathFinding[parent];
        }
    }
    private void createNetwork(uint[,] idGrid)
    {
        Vector3Int start = new Vector3Int(),
            end = new Vector3Int(),
            movement;
        int ix, iy;
        for (int x = 0; x < Width; ++x)
        {
            for (int y = 0; y < Length; ++y)
            {
                start.Set(x, y, pathFinding[idGrid[x, y]].Item.z);
                for (int i = -1; i <= 1; i += 2)
                {
                    ix = x + i;
                    iy = y + i;
                    if (ix < Width && ix >= 0)
                    {
                        end.Set(ix, y, pathFinding[idGrid[ix, y]].Item.z);
                        movement = end - start;
                        if (movement.z <= 0) pathFinding.Connect(idGrid[x, y], idGrid[ix, y], movementCostFunction(movement), 0);
                    }
                    if (iy < Length && iy >= 0)
                    {
                        end.Set(x, iy, pathFinding[idGrid[x, iy]].Item.z);
                        movement = end - start;
                        if (movement.z <= 0) pathFinding.Connect(idGrid[x, y], idGrid[x, iy], movementCostFunction(movement), 0);
                    }
                }
            }
        }
    }
    private void generateRivers()
    {
        // Reset
        RiverMap.ClearAllTiles();

        if (MaxRivers == 0) return;

        Vector3Int min = Floor.cellBounds.min;

        // Find travel nodes
        List<uint> SnowList = new List<uint>(),
            WaterList = new List<uint>();
        var nodes = pathFinding.GetEnumerator();
        while (nodes.MoveNext())
        {
            if (nodes.Current.Item.z == WaterLevel)
            {
                foreach (uint pId in pathFinding.Parents(nodes.Current.Key))
                {
                    if (pathFinding[pId].Item.z == WaterLevel + 1)
                    {
                        WaterList.Add(nodes.Current.Key);
                        break;
                    }
                }
            }
            // Any snow
            else if (nodes.Current.Item.z == SnowLevel) SnowList.Add(nodes.Current.Key);
            // Edges of snow
            // No edges moving upward, so no parents in lower z-axis
            /*else if (nodes.Current.Item.z == SnowLevel - 1)
            {
                foreach (uint pId in pathFinding.Parents(nodes.Current.Key))
                {
                    // One parent can have several children
                    if (pathFinding[pId].Item.z == SnowLevel && !SnowList.Contains(pId))
                    {
                        SnowList.Add(nodes.Current.Key);
                        break;
                    }
                }
            }*/
        }

        uint source, destination = uint.MaxValue;
        Vector3Int currentVector, lastVector = new Vector3Int(-1, -1, -1);
        int randomSnowIndex;
        Dictionary<uint, bool> obsticals = new Dictionary<uint, bool>();
        if (SnowList.Count > 0 && WaterList.Count > 0)
        {
            int minCount = SnowList.Count < WaterList.Count ? SnowList.Count : WaterList.Count;
            // Number of rivers
            for (int i = 0; i < Random.Range(MaxRivers / 2 > minCount ? minCount / 2 : MaxRivers / 2, MaxRivers > minCount ? minCount : MaxRivers); ++i)
            {
                // Source/Destination
                // <Todo>
                // For each river destination, expand the lake if small
                // </Todo>
                randomSnowIndex = Random.Range(0, SnowList.Count - 1);
                source = SnowList[randomSnowIndex];
                SnowList.RemoveAt(randomSnowIndex);
                currentVector = pathFinding[source].Item;
                Vector3Int minVector = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
                foreach (uint id in WaterList)
                {
                    if (destination == uint.MaxValue) destination = id;
                    else if ((pathFinding[id].Item - currentVector).magnitude
                        < (pathFinding[destination].Item - currentVector).magnitude)
                        destination = id;
                }
                WaterList.Remove(destination);

                var path = pathFinding.Dijkstra(source, destination);
                foreach (var node in path.GetPath())
                {
                    currentVector = pathFinding[node].Item;
                    TileBase tile = Floor.GetTile(currentVector);
                    if (lastVector.x == -1) lastVector = currentVector;
                    if (currentVector.z < WaterLevel || obsticals.ContainsKey(node)) break;
                    Floor.SetTile(currentVector, null);
                    setTile(RiverMap, currentVector, WaterTile);
                    --currentVector.z;
                    setTile(Floor, currentVector, tile);
                    lastVector = pathFinding[node].Item;
                    obsticals.Add(node, true); // removeParents(node);
                }
            }
            return;
        }
    }
    public void Generate()
    {
        Debug.ClearDeveloperConsole();
        var time = Time.realtimeSinceStartup;
        uint[,] idGrid = generateTerrain();
        time = Time.realtimeSinceStartup - time;
        if (time > 1) Debug.Log("Terrain generated in " + time);
        time = Time.realtimeSinceStartup;
        generateRivers();
        time = Time.realtimeSinceStartup - time;
        if (time > 1) Debug.Log("River generated in " + time);
        return;
    }
}
