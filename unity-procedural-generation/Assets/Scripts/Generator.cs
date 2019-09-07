using Dijkstra.NET.Graph;
using UnityEngine;

class Generator
{
    public Graph<Vector3Int, int> Graph;
    public virtual void Reset()
    {
        Graph = new Graph<Vector3Int, int>();
    }
}