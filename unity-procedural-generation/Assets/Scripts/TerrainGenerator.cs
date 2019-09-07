using UnityEngine;

class TerrainGenerator : Generator
{
    // Public members
    public int Width, Length;
    // Private members
    private PerlinNoise perlinNoise;

    // Constructors
    public TerrainGenerator(int width, int length)
    {
        Width = width;
        Length = length;
        perlinNoise = new PerlinNoise(width, length);
    }

    public void GenerateTerrain(int MinimumSmoothness, int MaximumSmoothness, float Lacunarity, float Amplitude, int Octaves)
    {
        var time = Time.realtimeSinceStartup;

        Reset();
        int smoothness = Random.Range(MinimumSmoothness, MaximumSmoothness),
            z;
        Vector3Int vectorIndex = new Vector3Int();
        for (int y = 0; y < Length; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                z = (int)Mathf.Round(
                    /* Setting impact:
                     * Frequency: smoothness of terrain
                     * Lacunarity: Randomness
                     * Amplitude: Length contrast
                     * Octaves: Edge smoothness
                     */
                    perlinNoise.FractionalBrownianMotion(
                        x, y,
                        frequency: (float)1 / smoothness,
                        lacunarity: Lacunarity,
                        amplitude: Amplitude,
                        octaves: Octaves
                    ) * 10);
                vectorIndex.Set(x, y, z);
                Graph.AddNode(vectorIndex);
            }
        }

        // Create network
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
        Debug.Log("Terrain generated in " + (Time.realtimeSinceStartup - time));
    }

    public override void Reset()
    {
        base.Reset();
        perlinNoise.ResetGradientArray();
    }

    private int costFunction(Vector3Int movementVector)
    {
        if (Mathf.Abs(movementVector.x) > 1 || Mathf.Abs(movementVector.y) > 1 || Mathf.Abs(movementVector.z) > 1) Debug.LogError("Moving more than one " + movementVector + ": " + (movementVector.z + Mathf.Abs(movementVector.y) + Mathf.Abs(movementVector.x)));
        return movementVector.z + Mathf.Abs(movementVector.y) + Mathf.Abs(movementVector.x);
    }
}
