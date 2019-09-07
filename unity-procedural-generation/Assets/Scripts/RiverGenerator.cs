using UnityEngine;
// Added
using System.Collections.Generic;
using Dijkstra.NET.ShortestPath;
using Dijkstra.NET.Graph;

class RiverGenerator : Generator
{
    private List<uint> obsticals = new List<uint>();

    public void GenerateRivers(Graph<Vector3Int, int> terrainGraph, List<uint> sourceList, List<uint> destinationList, int MaxRivers)
    {
        var time = Time.realtimeSinceStartup;
        Reset();
        if (MaxRivers == 0) return;

        // Find travel nodes
        uint source, destination = uint.MaxValue;
        Vector3Int currentVector, lastVector = new Vector3Int(-1, -1, -1);
        int randomSnowIndex;
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
                currentVector = terrainGraph[source].Item;
                Vector3Int minVector = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
                foreach (uint id in destinationList)
                {
                    if (destination == uint.MaxValue) destination = id;
                    else if ((terrainGraph[id].Item - currentVector).magnitude
                        < (terrainGraph[destination].Item - currentVector).magnitude)
                        destination = id;
                }
                destinationList.Remove(destination);
                generateRiver(terrainGraph, source, destination);
            }
        }

        Debug.Log("Rivers generated in " + (Time.realtimeSinceStartup - time));
        return;
    }

    private void generateRiver(Graph<Vector3Int, int> terrainGraph, uint source, uint destination)
    {
        Vector3Int currentVector;
        var path = terrainGraph.Dijkstra(source, destination);
        // bool firstNodePlaced = false;
        foreach (var node in path.GetPath())
        {
            /* if (!firstNodePlaced) Graph.AddNode(currentVector);
            else Graph.Connect(obsticals[obsticals.Count-1], Graph.AddNode(currentVector), 0, 0); */
            if (obsticals.Contains(node)) break;
            currentVector = terrainGraph[node].Item;
            Graph.AddNode(currentVector); // No need for edges
            obsticals.Add(node); // removeParents(node);
        }
    }

    public override void Reset()
    {
        base.Reset();
        obsticals.Clear();
    }
}