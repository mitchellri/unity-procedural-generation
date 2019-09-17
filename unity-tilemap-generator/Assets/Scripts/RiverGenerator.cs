using UnityEngine;
// Added
using System.Collections.Generic;
using Dijkstra.NET.ShortestPath;
using Dijkstra.NET.Graph;

class RiverGenerator : Generator
{
    // Private members
    private List<uint> obsticals; // Unaltered terrain nodes where rivers have been placed
    private const float riverDepth = 0.25f;
    private const int riverSlopeMagnitude = 2;

    public RiverGenerator(int width, int length) : base(width, length)
    {
        obsticals = new List<uint>();
        FillHeightMap(int.MaxValue);
    }

    // River methods
    public void GenerateRivers(TerrainGenerator terrainGenerator, List<uint> sourceList, List<uint> destinationList, int MaxRivers)
    {
        if (MaxRivers == 0) return;

        uint source, destination = uint.MaxValue;
        Vector3Int currentVector, lastVector = new Vector3Int(-1, -1, -1);
        int randomSnowIndex,
            lastObsticalLength = 0;
        if (sourceList.Count > 0 && destinationList.Count > 0)
        {
            int minCount = sourceList.Count < destinationList.Count ? sourceList.Count : destinationList.Count;
            // Number of rivers
            for (int i = 0; i < Random.Range(MaxRivers / 2 > minCount ? minCount / 2 : MaxRivers / 2, MaxRivers > minCount ? minCount : MaxRivers); ++i)
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
                GenerateRiver(terrainGenerator, source, destination);
                while (lastObsticalLength < obsticals.Count)
                    sourceList.Remove(obsticals[lastObsticalLength++]);
            }
            terrainGenerator.SetGraph(terrainGenerator.HeightMap);
        }
        return;
    }

    public void GenerateRiver(TerrainGenerator terrainGenerator, uint source, uint destination)
    {
        // Temporary variables
        var path = terrainGenerator.Graph.Dijkstra(source, destination);
        Vector3Int waterVector, previousWaterVector = new Vector3Int(-1, -1, -1),
            destinationVector = terrainGenerator.Graph[destination].Item;
        int terrainZ;

        foreach (var node in path.GetPath())
        {
            terrainZ = terrainGenerator.Graph[node].Item.z;
            waterVector = terrainGenerator.Graph[node].Item;
            waterVector.z -= (int)((waterVector.z - destinationVector.z) * riverDepth); // Lower river vector from terrain

            // Hit another river
            if (obsticals.Contains(node)) break;

            // Readjust terrain if going up
            if (previousWaterVector.x >= 0 // First node placed
                && (waterVector - previousWaterVector).z > 0) // Going up
                waterVector.z = previousWaterVector.z;

            // Add node
            Graph.AddNode(waterVector);
            HeightMap[waterVector.x, waterVector.y] = waterVector.z;
            obsticals.Add(node);
            previousWaterVector = waterVector;

            // Reform land
            terrainGenerator.HeightMap[waterVector.x, waterVector.y] = --waterVector.z;
            generateBank(terrainGenerator, node);
        }
    }

    // River bank methods
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
                    generateBank(terrainGenerator, parent); // Crashes Unity
                }
            }
        }
    }

    public override void Reset()
    {
        base.Reset();
        obsticals.Clear();
        FillHeightMap(int.MaxValue);
    }
}