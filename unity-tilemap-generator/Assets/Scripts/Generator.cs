using Dijkstra.NET.Graph;
using UnityEngine;

abstract class Generator
{
    public Graph<Vector3Int, int> Graph { get; protected set; }
    public int[,] HeightMap { get; protected set; }
    public int Width;
    public int Length;
    public Generator(int width, int length)
    {
        Width = width;
        Length = length;
        Graph = new Graph<Vector3Int, int>();
        HeightMap = new int[width, length];
    }
    public virtual void Reset()
    {
        Graph = new Graph<Vector3Int, int>();
    }
    protected void FillHeightMap(int value)
    {
        for (int i = 0; i < Width; ++i) for (int j = 0; j < Length; ++j) HeightMap[i, j] = value;
    }
}