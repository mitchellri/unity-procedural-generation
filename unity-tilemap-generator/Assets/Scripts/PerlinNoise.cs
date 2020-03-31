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

    float SmoothStep(float f)
    {
        return f * f * (3 - 2 * f);
    }
    float DotGradient(int i_x, int i_y, float o_x, float o_y)
    {
        Vector3 v = gradientArray[i_x, i_y];
        return o_x * v.x + o_y * v.y;
    }
    public float Perlin_Seamless(float x, float y, int array_period_x, int array_period_y)
    {
        if (array_period_x <= 0) array_period_x = gradientArray.GetLength(0);
        if (array_period_y <= 0) array_period_y = gradientArray.GetLength(1);
        // indexes
        int i_x0 = (int)x % array_period_x;
        int i_y0 = (int)y % array_period_y;
        int i_x1 = (i_x0 + 1) % array_period_x;
        int i_y1 = (i_y0 + 1) % array_period_y;
        // offsets
        float o_x0 = x % 1;
        float o_y0 = y % 1;
        float o_x1 = o_x0 - 1.0f;
        float o_y1 = o_y0 - 1.0f;
        // mix coefficients
        float c_x = SmoothStep(o_x0);
        float c_y = SmoothStep(o_y0);
        // values
        float v_bl = DotGradient(i_x0, i_y0, o_x0, o_y0);
        float v_br = DotGradient(i_x1, i_y0, o_x1, o_y0);
        float v_tl = DotGradient(i_x0, i_y1, o_x0, o_y1);
        float v_tr = DotGradient(i_x1, i_y1, o_x1, o_y1);
        float v_b = Mathf.Lerp(v_bl, v_br, c_x);
        float v_t = Mathf.Lerp(v_tl, v_tr, c_x);
        float v = Mathf.Lerp(v_b, v_t, c_y);
        return v;
    }
    public float FBM_Seamless(float x, float y, int octaves, float gain, float amplitude, int array_period_x, int array_period_y)
    {
        float sum = 0;
        float frequency = 0;
        int periodFrequency;
        int size = gradientArray.GetLength(0);
        if (array_period_x > 0) periodFrequency = array_period_x;
        else if (array_period_y > 0)
        {
            periodFrequency = array_period_y;
            size = gradientArray.GetLength(1);
        }
        else periodFrequency = 0;
        for (int i = 0; i < octaves && array_period_x < gradientArray.GetLength(0) && array_period_y < gradientArray.GetLength(1); ++i)
        {
            frequency = (float)periodFrequency / size;
            sum += amplitude * Perlin_Seamless(x * frequency, y * frequency, array_period_x, array_period_y);
            amplitude *= gain;
            periodFrequency *= 2;
            array_period_x *= 2;
            array_period_y *= 2;
        }
        return sum;
    }
    public float DW_Seamless(float x, float y, int octaves, float gain, float amplitude, int array_period_x, int array_period_y)
    {
        const float scale = 12.5f;
        const float offsetX = 69.420f;
        const float offsetY = 420.69f;

        // First iteration
        float wx = x + scale * FBM_Seamless(x, y, octaves, gain, amplitude, array_period_x, array_period_y);
        float wy = y + scale * FBM_Seamless(x + offsetX, y + offsetY, octaves, gain, amplitude, array_period_x, array_period_y);
        
        // Snap to edge
        if (wx > gradientArray.GetLength(0)) wx = gradientArray.GetLength(0);
        else if (wx < 0) wx = 0;
        if (wy > gradientArray.GetLength(1)) wy = gradientArray.GetLength(1);
        else if (wy < 0) wy = 0;

        return FBM_Seamless(wx, wy, octaves, gain, amplitude, array_period_x, array_period_y);
    }
}