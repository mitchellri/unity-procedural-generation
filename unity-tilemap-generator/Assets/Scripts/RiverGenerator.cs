using UnityEngine;
using System.Collections.Generic;
using Dijkstra.NET.ShortestPath;
using System.Linq;

class RiverGenerator : Generator
{
    // Private members
    private List<uint> obsticals; // Unaltered terrain nodes where rivers have been placed
    private const float riverDepth = 0.25f;
    private const int riverSlopeMagnitude = 2;

    public RiverGenerator(int width, int length) : base(width, length)
    {
        obsticals = new List<uint>();
        fillHeightMap(int.MaxValue);
    }

    // River methods
    /// <summary>
    /// Generate best path rivers from random source level nodes to closest destination level nodes
    /// </summary>
    /// <param name="terrainGenerator">Terrain to place rivers</param>
    /// <param name="minSourceLevel">Minimum z-level for river sources</param>
    /// <param name="destinationLevel">Z-level for river desintations</param>
    /// <param name="numRivers">Number of rivers to generate</param>
    public void GenerateRiversByPath(TerrainGenerator terrainGenerator, int minSourceLevel, int destinationLevel, int numRivers)
    {
        Reset();
        if (numRivers <= 0) return;

        // Create source and destination list
        var nodes = terrainGenerator.Graph.GetEnumerator();
        List<uint> sourceList = new List<uint>(),
            destinationList = new List<uint>();
        while (nodes.MoveNext())
        {
            if (nodes.Current.Item.z == destinationLevel)
            {
                foreach (uint pId in terrainGenerator.Graph.Parents(nodes.Current.Key))
                {
                    if (terrainGenerator.Graph[pId].Item.z == destinationLevel + 1)
                    {
                        destinationList.Add(nodes.Current.Key);
                    }
                }
            }
            // Any snow
            else if (nodes.Current.Item.z >= minSourceLevel) sourceList.Add(nodes.Current.Key);
        }

        // Temporary variables
        uint source, destination = uint.MaxValue;
        Vector3Int currentVector, lastVector = new Vector3Int(-1, -1, -1);
        int randomSnowIndex,
            lastObsticalLength = obsticals.Count;
        if (sourceList.Count > 0 && destinationList.Count > 0)
        {
            for (int i = 0; i < numRivers; ++i)
            {
                // Source/Destination
                randomSnowIndex = Random.Range(0, sourceList.Count - 1);
                source = sourceList[randomSnowIndex];
                sourceList.RemoveAt(randomSnowIndex);
                currentVector = terrainGenerator.Graph[source].Item;
                // Closest destination to source
                foreach (uint id in destinationList)
                {
                    if (destination == uint.MaxValue) destination = id;
                    else if ((terrainGenerator.Graph[id].Item - currentVector).magnitude
                        < (terrainGenerator.Graph[destination].Item - currentVector).magnitude)
                        destination = id;
                }
                destinationList.Remove(destination);
                GenerateRiverByPath(terrainGenerator, source, destination);
                while (lastObsticalLength < obsticals.Count)
                    sourceList.Remove(obsticals[lastObsticalLength++]);
            }
            terrainGenerator.SetGraph(terrainGenerator.HeightMap);
        }
        return;
    }

    /// <summary>
    /// Generate rivers from random source level nodes to closest lower level until destination level
    /// </summary>
    /// <param name="terrainGenerator">Terrain to place rivers</param>
    /// <param name="minSourceLevel">Minimum z-level for river sources</param>
    /// <param name="destinationLevel">Z-level for river desintations</param>
    /// <param name="numRivers">Number of rivers to generate</param>
    public void GenerateRiversByHeight(TerrainGenerator terrainGenerator, int minSourceLevel, int destinationLevel, int numRivers)
    {
        Reset();
        if (minSourceLevel < destinationLevel || numRivers <= 0) return;

        var nodes = terrainGenerator.Graph.GetEnumerator();
        Dictionary<int, List<uint>> levelNodes = new Dictionary<int, List<uint>>();
        while (nodes.MoveNext())
            if (terrainGenerator.Graph[nodes.Current.Key].Item.z >= destinationLevel || terrainGenerator.Graph[nodes.Current.Key].Item.z <= minSourceLevel)
            {
                if (!levelNodes.ContainsKey(terrainGenerator.Graph[nodes.Current.Key].Item.z))
                    levelNodes[terrainGenerator.Graph[nodes.Current.Key].Item.z] = new List<uint>();
                levelNodes[terrainGenerator.Graph[nodes.Current.Key].Item.z].Add(nodes.Current.Key);
            }
        int maxLevel = levelNodes.Keys.Max(),
            lastObsticalLength = obsticals.Count;

        if (maxLevel < minSourceLevel || !levelNodes.ContainsKey(destinationLevel)) return;

        int currentLevel;
        List<uint> nodeList;
        for (int i = 0; i < numRivers; ++i)
        {
            currentLevel = Random.Range(minSourceLevel, maxLevel);
            nodeList = levelNodes[currentLevel];
            GenerateRiverByHeight(terrainGenerator, nodeList[Random.Range(0, nodeList.Count - 1)], destinationLevel, levelNodes);
        }
        terrainGenerator.SetGraph(terrainGenerator.HeightMap);
    }

    /// <summary>
    /// Generate river from source to closest lower level until destination level
    /// </summary>
    /// <param name="terrainGenerator">Terrain to place river</param>
    /// <param name="source">Source node ID</param>
    /// <param name="destinationLevel">Z-level for river desintations</param>
    /// <param name="levelNodes">Optional z-level indexed dictionary of node ID lists (improves efficiency if reused)</param>
    /// <returns>True if river generates uninterrupted</returns>
    public bool GenerateRiverByHeight(TerrainGenerator terrainGenerator, uint source, int destinationLevel, Dictionary<int, List<uint>> levelNodes = null)
    {
        List<uint> nodeList;
        Vector3Int currentVector;
        uint destination;
        int sourceLevel = terrainGenerator.HeightMap[terrainGenerator.Graph[source].Item.x, terrainGenerator.Graph[source].Item.y];
        if (levelNodes is null)
        {
            var nodes = terrainGenerator.Graph.GetEnumerator();
            levelNodes = new Dictionary<int, List<uint>>();
            while (nodes.MoveNext())
                if (terrainGenerator.Graph[nodes.Current.Key].Item.z >= destinationLevel || terrainGenerator.Graph[nodes.Current.Key].Item.z <= sourceLevel)
                {
                    if (!levelNodes.ContainsKey(terrainGenerator.Graph[nodes.Current.Key].Item.z))
                        levelNodes[terrainGenerator.Graph[nodes.Current.Key].Item.z] = new List<uint>();
                    levelNodes[terrainGenerator.Graph[nodes.Current.Key].Item.z].Add(nodes.Current.Key);
                }
        }

        // Create path
        for (int currentLevel = terrainGenerator.Graph[source].Item.z; currentLevel > destinationLevel; --currentLevel)
        {
            // Source
            nodeList = levelNodes[currentLevel];
            currentVector = terrainGenerator.Graph[source].Item;

            destination = uint.MaxValue;
            if (currentLevel - 1 >= destinationLevel)
                nodeList = levelNodes[currentLevel - 1];
            // Closest lower level destination to source
            foreach (uint id in nodeList)
            {
                if (destination == uint.MaxValue) destination = id;
                else if ((terrainGenerator.Graph[id].Item - currentVector).magnitude
                    < (terrainGenerator.Graph[destination].Item - currentVector).magnitude)
                    destination = id;
            }
            nodeList.Remove(destination);

            if (!GenerateRiverByPath(terrainGenerator, source, destination, (int)levelNodes[destinationLevel][0])) return false;
            source = destination;
        }
        return true;
    }

    /// <summary>
    /// Generate river from source to closest lower level until destination level
    /// </summary>
    /// <param name="terrainGenerator">Terrain to place river</param>
    /// <param name="source">Source node ID</param>
    /// <param name="destination">Destination node ID</param>
    /// <param name="finilPathDestination">Destination node ID if destination parameter is not final destination</param>
    /// <returns></returns>
    public bool GenerateRiverByPath(TerrainGenerator terrainGenerator, uint source, uint destination, int? finalPathDestination = null)
    {
        if (source == destination) return true;

        // Temporary variables
        System.Func<uint, uint, int> heuristic = delegate (uint a, uint b)
        {
            return (int)(terrainGenerator.Graph[destination].Item - terrainGenerator.Graph[a].Item).magnitude;
        };
        Vector3Int waterVector, previousWaterVector = new Vector3Int(-1, -1, -1),
            destinationVector = finalPathDestination is null ?
                terrainGenerator.Graph[(uint)finalPathDestination].Item
                : terrainGenerator.Graph[destination].Item;
        int terrainZ;
        var path = terrainGenerator.Graph.AStar(source, destination, heuristic);
        foreach (var node in path.GetPath())
        {
            terrainZ = terrainGenerator.Graph[node].Item.z;
            waterVector = terrainGenerator.Graph[node].Item;
            waterVector.z -= (int)((waterVector.z - destinationVector.z) * riverDepth); // Lower river vector from terrain
            if (previousWaterVector.x < 0) previousWaterVector = waterVector;

            // Hit another river
            if (obsticals.Contains(node))
            {
                if (node == source) continue;
                else return false;
            }

            // Readjust terrain if going up
            if ((waterVector - previousWaterVector).z > 0)
                waterVector.z = previousWaterVector.z;

            // Add node
            Graph.AddNode(waterVector);
            HeightMap[waterVector.x, waterVector.y] = waterVector.z;
            obsticals.Add(node);
            previousWaterVector = waterVector;

            // Reform land
            terrainGenerator.HeightMap[waterVector.x, waterVector.y] = waterVector.z - 1;
            generateBank(terrainGenerator, node);
        }
        return true;
    }

    private void generateBank(TerrainGenerator terrainGenerator, uint node)
    {
        Vector3Int currentVector = terrainGenerator.Graph[node].Item,
            indexVector;
        currentVector.z = terrainGenerator.HeightMap[currentVector.x, currentVector.y];
        // River banks
        bool bAltered = true;
        while (bAltered)
        {
            // Our default assumption is that we don't change anything
            // so we don't need to repeat the process
            bAltered = false;
            // Cycle through all valid terrain within the slope width
            // of the current position
            foreach (uint parent in terrainGenerator.Graph.Parents(node))
            {
                if (obsticals.Contains(parent)) continue;
                indexVector = terrainGenerator.Graph[parent].Item;
                indexVector.z = terrainGenerator.HeightMap[indexVector.x, indexVector.y];
                // find the slope from where we are to where we are checking
                Vector3Int fSlope = currentVector - indexVector;
                if (fSlope.magnitude > riverSlopeMagnitude)
                {
                    // the slope is too big so adjust the height and keep in mind
                    // that the terrain was altered and we should make another pass
                    --terrainGenerator.HeightMap[indexVector.x, indexVector.y];
                    bAltered = true;
                    generateBank(terrainGenerator, parent);
                }
            }
        }
    }

    /// <summary>
    /// Sets graph, obsticals, heightmap to default values
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        obsticals.Clear();
        fillHeightMap(int.MaxValue);
    }
}