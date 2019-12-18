using UnityEngine;
using System.Collections.Generic;

class WaterGenerator : Generator
{
    public uint[,] idMap { get; private set; }
    public float MinHeight;
    public float MaxHeight;
    public WaterGenerator(int width, int length) : base(width, length)
    {
        idMap = new uint[width, length];
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;
    }

    /// <summary>
    /// Fills map with water
    /// </summary>
    public void Fill(TerrainGenerator terrainGenerator, float fillLevel)
    {
        if (MinHeight > fillLevel) MinHeight = fillLevel;
        if (MaxHeight < fillLevel) MaxHeight = fillLevel;
        var nodes = terrainGenerator.Graph.GetEnumerator();
        Vector3 vector = new Vector3();
        while (nodes.MoveNext())
            if (nodes.Current.Item.z < fillLevel)
            {
                vector.Set(nodes.Current.Item.x, nodes.Current.Item.y, fillLevel);
                idMap[(int)nodes.Current.Item.x, (int)nodes.Current.Item.y] = Graph.AddNode(vector);
            }
    }

    /// <summary>
    /// Creates rivers where excess wetness exists in terrain
    /// </summary>
    public void FillExcessWetness(TerrainGenerator terrainGenerator, float destinationLevel, float directionInertia = .1f, float sedimentDeposit = .1f,
        float minSlope = .1f, float sedimentCapacity = 10, float depositionSpeed = .02f, float erosionSpeed = .9f, float evaporationSpeed = .001f)
    {
        var nodes = terrainGenerator.Graph.GetEnumerator();
        Vector3 vector = new Vector3();
        int i, j;
        while (nodes.MoveNext())
        {
            i = (int)nodes.Current.Item.x;
            j = (int)nodes.Current.Item.y;
            if (terrainGenerator.HeightMap[i, j] >= destinationLevel
                && terrainGenerator.WetnessMap[i, j] > terrainGenerator.AbsorptionCapacity)
            {
                vector.Set(i, j, nodes.Current.Item.z + terrainGenerator.WetnessMap[i, j] - terrainGenerator.AbsorptionCapacity);
                idMap[i, j] = Graph.AddNode(vector);
                if (MinHeight > vector.z) MinHeight = vector.z;
                if (MaxHeight < vector.z) MaxHeight = vector.z;
            }
        }
    }

    /// <summary>
    /// Sets graph, obsticals, heightmap to default values
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        for (int i = 0; i < idMap.GetLength(0); ++i)
            for (int j = 0; j < idMap.GetLength(1); ++j)
                idMap[i, j] = 0;
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;
    }
}