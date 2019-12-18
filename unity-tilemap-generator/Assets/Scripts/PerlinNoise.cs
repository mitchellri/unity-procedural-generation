using UnityEngine;

public class PerlinNoise
{
    // Private members
    private static readonly int gradientRange = 100;
    private Vector3[,] gradientArray;

    public PerlinNoise(int width, int length)
    {
        gradientArray = new Vector3[width, length];
        ResetGradientArray();
    }
    /// <summary>
    /// Generates new noise array
    /// </summary>
    public void ResetGradientArray()
    {
        for (int i = 0; i < gradientArray.GetLength(0); ++i)
            for (int j = 0; j < gradientArray.GetLength(1); ++j)
                gradientArray[i, j] = new Vector3(
                    Random.Range(-gradientRange, gradientRange),
                    Random.Range(-gradientRange, gradientRange))
                    / gradientRange;
        return;
    }

    // Computes the dot product of the distance and gradient vectors.
    private float dotArrayGradient(int ix, int iy, float x, float y)
    {
        // Compute the distance vector
        float dx = x - (float)ix;
        float dy = y - (float)iy;

        int xInd = ix % gradientArray.GetLength(0),
            yInd = iy % gradientArray.GetLength(1);

        if (xInd < 0) xInd = 0;
        if (yInd < 0) yInd = 0;

        // Compute the dot-product
        return dx * gradientArray[xInd, yInd].x
            + dy * gradientArray[xInd, yInd].y;
    }
    /// <summary>
    /// Perlin noise value for 2D coordinate
    /// </summary>
    /// <returns>Noise value</returns>
    public float Perlin(float x, float y)
    {
        // Determine grid cell coordinates
        int x0 = (int)x;
        int x1 = x0 + 1;
        int y0 = (int)y;
        int y1 = y0 + 1;

        // Determine interpolation weights
        // Could also use higher order polynomial/s-curve here
        float sx = x - (float)x0;
        float sy = y - (float)y0;

        // Interpolate between grid point gradients
        float n0, n1, ix0, ix1, value;
        n0 = dotArrayGradient(x0, y0, x, y);
        n1 = dotArrayGradient(x1, y0, x, y);
        ix0 = Mathf.Lerp(n0, n1, sx);

        n0 = dotArrayGradient(x0, y1, x, y);
        n1 = dotArrayGradient(x1, y1, x, y);
        ix1 = Mathf.Lerp(n0, n1, sx);
        value = Mathf.Lerp(ix0, ix1, sy);
        return value;
    }

    /// <summary>
    /// Iterated perlin noise value for 2D coordinate
    /// </summary>
    /// <returns>Noise value</returns>
    public float FractionalBrownianMotion(float x, float y, int octaves, float lacunarity, float gain, float amplitude, float frequency)
    {
        float sum = 0;
        for (int i = 0; i < octaves; ++i)
        {
            sum += amplitude * Perlin(x * frequency, y * frequency);
            amplitude *= gain;
            frequency *= lacunarity;
        }
        return sum;
    }

    /// <summary>
    /// Warped fractional brownian motion noise value for 2D coordinate
    /// </summary>
    /// <returns>Noise value</returns>
    public float DomainWarp(float x, float y, int octaves, float lacunarity, float gain, float amplitude, float frequency)
    {
        const float scale = 50f, // 12.5 if using second iteration
            offsetX = 69.420f / 2,
            offsetY = 420.69f / 2;
        // First iteration
        float wx = x + scale * (1f - 2f * FractionalBrownianMotion(x + offsetX, y + offsetY, octaves, lacunarity, gain, amplitude, frequency));
        float wy = y + scale * (1f - 2f * FractionalBrownianMotion(x + offsetX, y + offsetY, octaves, lacunarity, gain, amplitude, frequency));
        // Second iteration
        // Does not look good
        /*wx = x + scaleX * FractionalBrownianMotion(x + scaleX * wx + xOffsetX2, y + scaleX * wy + xOffsetY2, octaves, lacunarity, gain, amplitude, frequency);
        wy = y + scaleY * FractionalBrownianMotion(x + scaleY * wx + yOffsetX2, y + scaleY * wy + yOffsetY2, octaves, lacunarity, gain, amplitude, frequency);*/
        if (wx < 0) wx = 0;
        if (wy < 0) wy = 0;
        if (wx > gradientArray.Length) wx = gradientArray.Length - 1;
        if (wy > gradientArray.Length) wy = gradientArray.Length - 1;
        return FractionalBrownianMotion(wx, wy, octaves, lacunarity, gain, amplitude, frequency);
    }
}