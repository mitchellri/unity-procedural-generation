using UnityEngine;
using System.Collections.Generic;

class WaterGenerator : Generator
{
    public float[,] HeightMap { get; private set; }
    public float MinHeight;
    public float MaxHeight;
    public WaterGenerator(int width, int length, int height) : base(width, length, height)
    {
        HeightMap = new float[width, length];
        Reset();
    }

    /// <summary>
    /// Fills map with water
    /// </summary>
    public void Fill(TerrainGenerator terrainGenerator, float fillLevel)
    {
        if (MinHeight > fillLevel) MinHeight = fillLevel;
        if (MaxHeight < fillLevel) MaxHeight = fillLevel;
        for (int x = 0; x < Width; ++x)
            for (int y = 0; y < Length; ++y)
                if (terrainGenerator.HeightMap[x, y] < fillLevel)
                    HeightMap[x, y] = fillLevel;
    }

    /// <summary>
    /// Creates rivers where excess wetness exists in terrain
    /// </summary>
    public void FillExcessWetness(TerrainGenerator terrainGenerator, float destinationLevel, float directionInertia, float sedimentDeposit,
        float minSlope, float sedimentCapacity, float depositionSpeed, float erosionSpeed, float evaporationSpeed)
    {
        for (int x = 0; x < Width; ++x)
            for (int y = 0; y < Length; ++y)
                if (terrainGenerator.HeightMap[x, y] >= destinationLevel
                    && terrainGenerator.WetnessMap[x, y] > terrainGenerator.AbsorptionCapacity)
                {
                    HeightMap[x, y] = terrainGenerator.HeightMap[x, y] + terrainGenerator.WetnessMap[x, y] - terrainGenerator.AbsorptionCapacity;
                    if (MinHeight > HeightMap[x, y]) MinHeight = HeightMap[x, y];
                    if (MaxHeight < HeightMap[x, y]) MaxHeight = HeightMap[x, y];
                }
    }

    /// <summary>
    /// Sets HeightMap, man and max height to default values
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        for (int i = 0; i < HeightMap.GetLength(0); ++i)
            for (int j = 0; j < HeightMap.GetLength(1); ++j)
                HeightMap[i, j] = float.MinValue;
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;
    }
}