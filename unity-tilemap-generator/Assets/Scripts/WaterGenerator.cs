using UnityEngine;
using System.Collections.Generic;
using Dijkstra.NET.ShortestPath;
using System.Linq;

class WaterGenerator : Generator
{
    // Private members
    private List<uint> obsticals; // Terrain nodes where rivers have been placed
    private const float riverDepth = 0.75f;
    private const int riverSlopeMagnitude = 2;

    public WaterGenerator(int width, int length) : base(width, length)
    {
        obsticals = new List<uint>();
    }

    // River methods
    /// <summary>
    /// Generate best path rivers from random source level nodes to closest destination level nodes
    /// </summary>
    /// <param name="terrainGenerator">Terrain to place rivers</param>
    /// <param name="minSourceLevel">Minimum z-level for river sources</param>
    /// <param name="destinationLevel">Z-level for river desintations</param>
    /// <param name="numRivers">Number of rivers to generate</param>
    public void RiversByPath(TerrainGenerator terrainGenerator, int minSourceLevel, int destinationLevel, int numRivers)
    {
        if (numRivers <= 0) return;

        // Create source and destination list
        var nodes = terrainGenerator.Graph.GetEnumerator();
        List<uint> sourceList = new List<uint>(),
            destinationList = new List<uint>();
        while (nodes.MoveNext())
        {
            if (nodes.Current.Item.z == destinationLevel - 1)
            {
                foreach (uint pId in terrainGenerator.Graph.Parents(nodes.Current.Key))
                {
                    if (terrainGenerator.Graph[pId].Item.z == destinationLevel)
                    {
                        destinationList.Add(pId);
                    }
                }
            }
            // Any snow
            else if (nodes.Current.Item.z >= minSourceLevel) sourceList.Add(nodes.Current.Key);
        }

        // Temporary variables
        uint source, destination = 0;
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
                    if (destination == 0) destination = id;
                    else if ((terrainGenerator.Graph[id].Item - currentVector).magnitude
                        < (terrainGenerator.Graph[destination].Item - currentVector).magnitude)
                        destination = id;
                }
                destinationList.Remove(destination);
                RiverByPath(terrainGenerator, source, destination);
                while (lastObsticalLength < obsticals.Count)
                    sourceList.Remove(obsticals[lastObsticalLength++]);
            }
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
    public void RiversByHeight(TerrainGenerator terrainGenerator, int minSourceLevel, int destinationLevel, int numRivers)
    {
        if (numRivers <= 0) return;
        if (minSourceLevel < destinationLevel) minSourceLevel = destinationLevel;

        var nodes = terrainGenerator.Graph.GetEnumerator();
        Dictionary<int, List<uint>> levelNodes = new Dictionary<int, List<uint>>();
        while (nodes.MoveNext())
            if (terrainGenerator.Graph[nodes.Current.Key].Item.z >= destinationLevel - 1)
            {
                if (!levelNodes.ContainsKey(terrainGenerator.Graph[nodes.Current.Key].Item.z))
                    levelNodes[terrainGenerator.Graph[nodes.Current.Key].Item.z] = new List<uint>();
                levelNodes[terrainGenerator.Graph[nodes.Current.Key].Item.z].Add(nodes.Current.Key);
            }
        int maxLevel = levelNodes.Keys.Max(),
            lastObsticalLength;

        if (maxLevel < minSourceLevel || !levelNodes.ContainsKey(destinationLevel - 1)) return;

        int nodeListIndex;
        List<uint> nodeList;
        for (int i = 0; i < numRivers; ++i)
        {
            lastObsticalLength = obsticals.Count;
            nodeList = levelNodes[Random.Range(minSourceLevel, maxLevel)];
            if (nodeList.Count <= 0) continue;
            nodeListIndex = Random.Range(0, nodeList.Count - 1);
            RiverByHeight(terrainGenerator, nodeList[nodeListIndex], destinationLevel, levelNodes);
            nodeList.RemoveAt(nodeListIndex);
            while (lastObsticalLength < obsticals.Count)
                if (levelNodes.ContainsKey(nodeListIndex = terrainGenerator.Graph[obsticals[lastObsticalLength]].Item.z))
                    levelNodes[nodeListIndex].Remove(obsticals[lastObsticalLength++]);
        }
    }

    /// <summary>
    /// Generate river from source to closest lower level until destination level
    /// </summary>
    /// <param name="terrainGenerator">Terrain to place river</param>
    /// <param name="source">Source node ID</param>
    /// <param name="destinationLevel">Z-level for river desintations</param>
    /// <param name="levelNodes">Optional z-level indexed dictionary of node ID lists (improves efficiency if reused)</param>
    /// <returns>True if river generates uninterrupted</returns>
    public bool RiverByHeight(TerrainGenerator terrainGenerator, uint source, int destinationLevel, Dictionary<int, List<uint>> levelNodes = null)
    {
        List<uint> nodeList;
        Vector3Int currentVector;
        uint destination, parentDestination;
        int sourceLevel = terrainGenerator.Graph[source].Item.z;
        if (levelNodes is null)
        {
            var nodes = terrainGenerator.Graph.GetEnumerator();
            levelNodes = new Dictionary<int, List<uint>>();
            while (nodes.MoveNext())
                if (terrainGenerator.Graph[nodes.Current.Key].Item.z >= destinationLevel - 1
                    || terrainGenerator.Graph[nodes.Current.Key].Item.z <= sourceLevel)
                {
                    if (!levelNodes.ContainsKey(terrainGenerator.Graph[nodes.Current.Key].Item.z))
                        levelNodes[terrainGenerator.Graph[nodes.Current.Key].Item.z] = new List<uint>();
                    levelNodes[terrainGenerator.Graph[nodes.Current.Key].Item.z].Add(nodes.Current.Key);
                }
        }

        // Create path
        for (int currentLevel = sourceLevel; currentLevel >= destinationLevel; --currentLevel)
        {
            // Source
            nodeList = levelNodes[currentLevel];
            currentVector = terrainGenerator.Graph[source].Item;

            destination = 0;
            if (currentLevel >= destinationLevel - 1)
                nodeList = levelNodes[currentLevel - 1];

            // Closest lower level destination to source
            parentDestination = 0;
            foreach (uint id in nodeList)
            {
                if (destination == 0) destination = id;
                else if ((terrainGenerator.Graph[id].Item - currentVector).magnitude
                    < (terrainGenerator.Graph[destination].Item - currentVector).magnitude)
                    destination = id;
            }
            nodeList.Remove(destination);

            foreach (uint parent in terrainGenerator.Graph.Parents(destination)) // More than 1 parent
                if (terrainGenerator.Graph[parent].Item.z == currentLevel)
                {
                    parentDestination = parent;
                    break;
                }
            if (parentDestination > 0)
            {
                if (!RiverByPath(terrainGenerator, source, parentDestination, levelNodes[destinationLevel + 1][0])) return false;
            }
            else if (!RiverByPath(terrainGenerator, source, destination, levelNodes[destinationLevel + 1][0])) return false;

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
    /// <param name="finalPathDestination">Destination node ID if destination parameter is not final destination</param>
    /// <returns></returns>
    public bool RiverByPath(TerrainGenerator terrainGenerator, uint source, uint destination, uint? finalPathDestination = null)
    {
        if (source == destination) return true;
        // Temporary variables
        System.Func<uint, uint, int> heuristic = delegate (uint a, uint b)
        {
            return (int)(terrainGenerator.Graph[destination].Item - terrainGenerator.Graph[a].Item).magnitude;
        };
        Vector3Int waterVector, previousWaterVector = new Vector3Int(-1, -1, -1),
            destinationVector = finalPathDestination is null ?
                terrainGenerator.Graph[destination].Item
                : terrainGenerator.Graph[(uint)finalPathDestination].Item;
        var path = terrainGenerator.Graph.AStar(source, destination, heuristic);
        foreach (var node in path.GetPath())
        {
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
            if (waterVector.z > previousWaterVector.z)
                waterVector.z = previousWaterVector.z;

            // Add node
            Graph.AddNode(waterVector);
            obsticals.Add(node);
            previousWaterVector = waterVector;

            // Reform land
            --waterVector.z;
            terrainGenerator.Graph[node].Item = waterVector;
            terrainGenerator.HeightMap[waterVector.x, waterVector.y] = waterVector.z;
            generateBank(terrainGenerator, node);
        }
        return true;
    }

    public void Fill(TerrainGenerator terrainGenerator, int fillLevel)
    {
        var nodes = terrainGenerator.Graph.GetEnumerator();
        Vector3Int vector;
        while (nodes.MoveNext())
            if (nodes.Current.Item.z < fillLevel)
                for (int z = nodes.Current.Item.z + 1; z <= fillLevel; ++z)
                {
                    vector = nodes.Current.Item;
                    vector.z = z;
                    Graph.AddNode(vector);
                }
    }

    private void generateBank(TerrainGenerator terrainGenerator, uint node, List<uint> visited = null)
    {
        if (visited is null) visited = new List<uint>();
        visited.Add(node);
        Vector3Int currentVector = terrainGenerator.Graph[node].Item,
            indexVector;
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
                if (visited.Contains(parent) || obsticals.Contains(parent)) continue;
                indexVector = terrainGenerator.Graph[parent].Item;
                // find the slope from where we are to where we are checking
                if ((currentVector - indexVector).magnitude > riverSlopeMagnitude)
                {
                    // the slope is too big so adjust the height and keep in mind
                    // that the terrain was altered and we should make another pass
                    bAltered = true;
                    terrainGenerator.Graph[parent].Item = new Vector3Int(indexVector.x,
                        indexVector.y,
                        --terrainGenerator.HeightMap[indexVector.x, indexVector.y]);
                    generateBank(terrainGenerator, parent, visited);
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
        obsticals.TrimExcess();
    }
}