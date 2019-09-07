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
        uint[,] idGrid = new uint[Width, Length];
        int smoothness = Random.Range(MinimumSmoothness, MaximumSmoothness),
            z;
        Vector3Int index = new Vector3Int();
        for (int x = 0; x < Width; ++x)
        {
            for (int y = 0; y < Length; ++y)
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
                index.Set(x, y, z);
                idGrid[x, y] = Graph.AddNode(index);
            }
        }
        createNetwork(idGrid);

        Debug.Log("Terrain generated in " + (Time.realtimeSinceStartup - time));
    }

    public override void Reset()
    {
        base.Reset();
        perlinNoise.ResetGradientArray();
    }

    private int movementCostFunction(Vector3Int movementVector)
    {
        return movementVector.z + Mathf.Abs(movementVector.y) + Mathf.Abs(movementVector.x);
    }

    private void createNetwork(uint[,] idGrid)
    {
        Vector3Int start = new Vector3Int(),
            end = new Vector3Int(),
            movement;
        int ix, iy;
        for (int x = 0; x < Width; ++x)
        {
            for (int y = 0; y < Length; ++y)
            {
                start.Set(x, y, Graph[idGrid[x, y]].Item.z);
                for (int i = -1; i <= 1; i += 2)
                {
                    ix = x + i;
                    iy = y + i;
                    if (ix < Width && ix >= 0)
                    {
                        end.Set(ix, y, Graph[idGrid[ix, y]].Item.z);
                        movement = end - start;
                        if (movement.z <= 0) Graph.Connect(idGrid[x, y], idGrid[ix, y], movementCostFunction(movement), 0);
                    }
                    if (iy < Length && iy >= 0)
                    {
                        end.Set(x, iy, Graph[idGrid[x, iy]].Item.z);
                        movement = end - start;
                        if (movement.z <= 0) Graph.Connect(idGrid[x, y], idGrid[x, iy], movementCostFunction(movement), 0);
                    }
                }
            }
        }
    }
}
