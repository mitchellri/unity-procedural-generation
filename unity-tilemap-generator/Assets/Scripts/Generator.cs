using Dijkstra.NET.Graph;
using UnityEngine;

abstract class Generator
{
    public Graph<Vector3Int, int> Graph { get; protected set; }
    public int Width;
    public int Length;
    public Generator(int width, int length)
    {
        Width = width;
        Length = length;
        Graph = new Graph<Vector3Int, int>();
    }

    /// <summary>
    /// Replaces graph
    /// </summary>
    public virtual void Reset()
    {
        Graph = new Graph<Vector3Int, int>();
    }
}