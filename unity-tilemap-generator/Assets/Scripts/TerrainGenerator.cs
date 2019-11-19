using Dijkstra.NET.Graph;
using UnityEngine;
using UnityEngine.Tilemaps;

class TerrainGenerator : Generator
{
    // Public members
    public float[,] HeightMap { get; private set; }
    public float MinHeight { get; private set; }
    public float MaxHeight { get; private set; }
    // Private members
    private PerlinNoise perlinNoise;

    // Constructors
    public TerrainGenerator(int width, int length) : base(width, length)
    {
        perlinNoise = new PerlinNoise(width, length);
        HeightMap = new float[width, length];
    }

    /// <summary>
    /// Generates terrain to class attributes
    /// </summary>
    public void GenerateTerrain(int smoothness, float lacunarity, float amplitude, int octaves, float scale = 10f)
    {
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;
        base.Reset();
        float z;
        Vector3 vectorIndex = new Vector3();
        for (int y = 0; y < Length; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                z = perlinNoise.DomainWarp(
                        x, y,
                        frequency: (float)1 / smoothness,
                        lacunarity: lacunarity,
                        amplitude: amplitude,
                        octaves: octaves
                    ) * scale;
                if (MinHeight > z) MinHeight = z;
                if (MaxHeight < z) MaxHeight = z;
                vectorIndex.Set(x, y, z);
                HeightMap[x, y] = z;
                Graph.AddNode(vectorIndex);
            }
        }
        setNetwork();
    }

    protected void setNetwork()
    {
        int ix, iy;
        for (uint i = 1; i <= Width * Length; ++i)
        {
            ix = (int)((i - 1) % Width);
            iy = (int)((i - 1) / Width);
            if (ix + 1 < Width) Graph.Connect(i, i + 1, costFunction(Graph[i + 1].Item - Graph[i].Item), 0);
            if (ix - 1 >= 0) Graph.Connect(i, i - 1, costFunction(Graph[i - 1].Item - Graph[i].Item), 0);
            if (iy + 1 < Length) Graph.Connect(i, (uint)(i + Width), costFunction(Graph[(uint)(i + Width)].Item - Graph[i].Item), 0);
            if (iy - 1 >= 0) Graph.Connect(i, (uint)(i - Width), costFunction(Graph[(uint)(i - Width)].Item - Graph[i].Item), 0);
        }
    }

    /// <summary>
    /// Resets noise
    /// </summary>
    public override void Reset()
    {
        // Base reset not required, is reset on generation
        //perlinNoise.ResetGradientArray();
        // Heightmap reset not required, it is all overwritten
    }

    private int costFunction(Vector3 movementVector)
    {
        return Mathf.RoundToInt(movementVector.z > 0 ? 999 : movementVector.z + Mathf.Abs(movementVector.y) + Mathf.Abs(movementVector.x));
    }
}
