using UnityEngine;

public class PerlinNoise
{
    // Private members
    private static readonly int gradientRange = 100;
    private Vector3[,,] gradientArray;

    public PerlinNoise(int width, int length, int height)
    {
        gradientArray = new Vector3[width, length, height];
        ResetGradientArray();
    }
    /// <summary>
    /// Generates new noise array
    /// </summary>
    public void ResetGradientArray()
    {
        for (int i = 0; i < gradientArray.GetLength(0); ++i)
            for (int j = 0; j < gradientArray.GetLength(1); ++j)
                for (int k = 0; k < gradientArray.GetLength(2); ++k)
                    gradientArray[i, j, k] = new Vector3(
                        Random.Range(-gradientRange, gradientRange),
                        Random.Range(-gradientRange, gradientRange),
                        Random.Range(-gradientRange, gradientRange))
                        / gradientRange;
        return;
    }

    // Computes the dot product of the distance and gradient vectors.
    private float dotArrayGradient(int ix, int iy, int iz, float x, float y, float z)
    {
        // Compute the distance vector
        float dx = x - (float)ix;
        float dy = y - (float)iy;
        float dz = z - (float)iz;

        int xInd = ix % gradientArray.GetLength(0),
            yInd = iy % gradientArray.GetLength(1),
            zInd = iz % gradientArray.GetLength(2);

        if (xInd < 0) xInd = 0;
        if (yInd < 0) yInd = 0;
        if (zInd < 0) zInd = 0;

        // Compute the dot-product
        return dx * gradientArray[xInd, yInd, zInd].x
            + dy * gradientArray[xInd, yInd, zInd].y
            + dz * gradientArray[xInd, yInd, zInd].z;
    }
    /// <summary>
    /// Perlin noise value for 2D coordinate
    /// </summary>
    /// <returns>Noise value</returns>
    public float Perlin(float x, float y, float z)
    {
        // Determine grid cell coordinates
        int x0 = (int)x;
        int x1 = x0 + 1;
        int y0 = (int)y;
        int y1 = y0 + 1;
        int z0 = (int)z;
        int z1 = z0 + 1;

        // Determine interpolation weights
        // Could also use higher order polynomial/s-curve here
        float sx = x - (float)x0;
        float sy = y - (float)y0;
        float sz = z - (float)z0;

        // Interpolate between grid point gradients
        float xyz = dotArrayGradient(x0, y0, z0, x, y, z),
            Xyz = dotArrayGradient(x1, y0, z0, x, y, z),
             XYz = dotArrayGradient(x1, y1, z0, x, y, z),
            XyZ = dotArrayGradient(x1, y0, z1, x, y, z),
             XYZ = dotArrayGradient(x1, y1, z1, x, y, z),
            xYz = dotArrayGradient(x0, y1, z0, x, y, z),
            xYZ = dotArrayGradient(x0, y1, z1, x, y, z),
            xyZ = dotArrayGradient(x0, y0, z1, x, y, z);

        /*      +---ZY----+
               /|        /|
              xZ|       xZ|
             /  v      /  v
            +---Zy----+   |
            |   |     |   |
            |   +--zY-|---+
            v  /      v  /
            | xz      | xz
            |/        |/
            +---zy----+  */
        float zy = Mathf.Lerp(xyz, Xyz, sx),
            zY = Mathf.Lerp(xYz, XYz, sx),
            Zy = Mathf.Lerp(xyZ, XyZ, sx),
            ZY = Mathf.Lerp(xYZ, XYZ, sx),
            xz = Mathf.Lerp(zy, zY, sy),
            xZ = Mathf.Lerp(Zy, ZY, sy),
            value = Mathf.Lerp(xz, xZ, sz);

        return value;
    }

    /// <summary>
    /// Iterated perlin noise value for 3D coordinate
    /// </summary>
    /// <returns>Noise value</returns>
    public float FractionalBrownianMotion(float x, float y, float z, int octaves, float lacunarity, float gain, float amplitude, float frequency)
    {
        float sum = 0;
        for (int i = 0; i < octaves; ++i)
        {
            sum += amplitude * Perlin(x * frequency, y * frequency, z * frequency);
            amplitude *= gain;
            frequency *= lacunarity;
        }
        return sum;
    }

    /// <summary>
    /// Warped fractional brownian motion noise value for 2D coordinate
    /// </summary>
    /// <returns>Noise value</returns>
    public float DomainWarp(float x, float y, float z, int octaves, float lacunarity, float gain, float amplitude, float frequency)
    {
        const float scale = 50f, // 12.5 if using second iteration
            offsetX = 69.420f / 2,
            offsetY = 420.69f / 2,
            offsetZ = (offsetY + offsetX) / 2;
        // First iteration
        float fbmValue = (1f - 2f * FractionalBrownianMotion(x + offsetX, y + offsetY, z + offsetZ, octaves, lacunarity, gain, amplitude, frequency));
        float wx = x + scale * fbmValue;
        float wy = y + scale * fbmValue;
        float wz = z + scale * fbmValue;
        // Second iteration
        // Does not look good
        /*wx = x + scaleX * FractionalBrownianMotion(x + scaleX * wx + xOffsetX2, y + scaleX * wy + xOffsetY2, octaves, lacunarity, gain, amplitude, frequency);
        wy = y + scaleY * FractionalBrownianMotion(x + scaleY * wx + yOffsetX2, y + scaleY * wy + yOffsetY2, octaves, lacunarity, gain, amplitude, frequency);*/
        if (wx < 0) wx = 0;
        if (wy < 0) wy = 0;
        if (wz < 0) wz = 0;
        // Using the real sizes results in weird generations for some reason
        /*if (wx > gradientArray.GetLength(0)) wx = gradientArray.GetLength(0) - 1;
        if (wy > gradientArray.GetLength(1)) wy = gradientArray.GetLength(1) - 1;
        if (wz > gradientArray.GetLength(2)) wz = gradientArray.GetLength(2) - 1;*/
        if (wx > gradientArray.Length) wx = gradientArray.Length - 1;
        if (wy > gradientArray.Length) wy = gradientArray.Length - 1;
        if (wz > gradientArray.Length) wz = gradientArray.Length - 1;
        return FractionalBrownianMotion(wx, wy, wz, octaves, lacunarity, gain, amplitude, frequency);
    }

    float SmoothStep(float f)
    {
        return f * f * (3 - 2 * f);
    }
    float DotGradient(int i_x, int i_y, int i_z, float o_x, float o_y, float o_z)
    {
        Vector3 v = gradientArray[i_x, i_y, i_z];
        return o_x * v.x + o_y * v.y + o_z * v.z;
    }

    /// <summary>
    /// Perlin noise seamless in x and/or y axis
    /// </summary>
    public float Perlin_Seamless(float x, float y, float z, int array_period_x, int array_period_y, int array_period_z)
    {
        if (array_period_x <= 0) array_period_x = gradientArray.GetLength(0);
        if (array_period_y <= 0) array_period_y = gradientArray.GetLength(1);
        if (array_period_z <= 0) array_period_z = gradientArray.GetLength(2);
        // indexes
        int i_x0 = (int)x % array_period_x;
        int i_y0 = (int)y % array_period_y;
        int i_z0 = (int)z % array_period_z;
        int i_x1 = (i_x0 + 1) % array_period_x;
        int i_y1 = (i_y0 + 1) % array_period_y;
        int i_z1 = (i_z0 + 1) % array_period_z;
        // offsets
        float o_x0 = x % 1;
        float o_y0 = y % 1;
        float o_z0 = z % 1;
        float o_x1 = o_x0 - 1.0f;
        float o_y1 = o_y0 - 1.0f;
        float o_z1 = o_z0 - 1.0f;
        // mix coefficients
        float c_x = SmoothStep(o_x0);
        float c_y = SmoothStep(o_y0);
        float c_z = SmoothStep(o_z0);
        // values
        /*float v_bl = DotGradient(i_x0, i_y0, o_x0, o_y0);
        float v_br = DotGradient(i_x1, i_y0, o_x1, o_y0);
        float v_tl = DotGradient(i_x0, i_y1, o_x0, o_y1);
        float v_tr = DotGradient(i_x1, i_y1, o_x1, o_y1);*/

        /*  +--v_t--+
            |       |
            v       v
            |       |
            +--v_b--+ */
        /*float v_b = Mathf.Lerp(v_bl, v_br, c_x);
        float v_t = Mathf.Lerp(v_tl, v_tr, c_x);
        float v = Mathf.Lerp(v_b, v_t, c_y);*/

        // values
        float xyz = DotGradient(i_x0, i_y0, i_z0, o_x0, o_y0, o_z0),
            Xyz = DotGradient(i_x1, i_y0, i_z0, o_x1, o_y0, o_z0),
             XYz = DotGradient(i_x1, i_y1, i_z0, o_x1, o_y1, o_z0),
            XyZ = DotGradient(i_x1, i_y0, i_z1, o_x1, o_y0, o_z1),
             XYZ = DotGradient(i_x1, i_y1, i_z1, o_x1, o_y1, o_z1),
            xYz = DotGradient(i_x0, i_y1, i_z0, o_x0, o_y1, o_z0),
            xYZ = DotGradient(i_x0, i_y1, i_z1, o_x0, o_y1, o_z1),
            xyZ = DotGradient(i_x0, i_y0, i_z1, o_x0, o_y0, o_z1);

        /*      +---ZY----+
               /|        /|
              xZ|       xZ|
             /  v      /  v
            +---Zy----+   |
            |   |     |   |
            |   +--zY-|---+
            v  /      v  /
            | xz      | xz
            |/        |/
            +---zy----+  */
        float zy = Mathf.Lerp(xyz, Xyz, c_x),
            zY = Mathf.Lerp(xYz, XYz, c_x),
            Zy = Mathf.Lerp(xyZ, XyZ, c_x),
            ZY = Mathf.Lerp(xYZ, XYZ, c_x),
            xz = Mathf.Lerp(zy, zY, c_y),
            xZ = Mathf.Lerp(Zy, ZY, c_y),
            v = Mathf.Lerp(xz, xZ, c_z);

        return v;
    }

    public float FBM_Seamless(float x, float y, float z, int octaves, float gain, float amplitude, int array_period_x, int array_period_y, int array_period_z)
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
            sum += amplitude * Perlin_Seamless(x * frequency, y * frequency, z * frequency, array_period_x, array_period_y, array_period_z);
            amplitude *= gain;
            periodFrequency *= 2;
            array_period_x *= 2;
            array_period_y *= 2;
        }
        return sum;
    }
    public float DW_Seamless(float x, float y, float z, int octaves, float gain, float amplitude, int array_period_x, int array_period_y, int array_period_z)
    {
        const float scale = 12.5f;
        const float offsetX = 69.420f;
        const float offsetY = 420.69f;
        const float offsetZ = (offsetX + offsetY) / 2;

        // First iteration
        float fbmValue = FBM_Seamless(x + offsetX, y + offsetY, z + offsetZ, octaves, gain, amplitude, array_period_x, array_period_y, array_period_z);
        float wx = x + scale * fbmValue;
        float wy = y + scale * fbmValue;
        float wz = z + scale * fbmValue;

        // Using the real sizes results in weird generations for some reason
        /*if (wx > gradientArray.GetLength(0)) wx = gradientArray.GetLength(0);
        else if (wx < 0) wx = 0;
        if (wy > gradientArray.GetLength(1)) wy = gradientArray.GetLength(1);
        else if (wy < 0) wy = 0;
        if (wz > gradientArray.GetLength(2)) wz = gradientArray.GetLength(2);
        else if (wz < 0) wz = 0;*/

        // Snap to edge
        if (wx > gradientArray.Length) wx = gradientArray.Length - 1;
        else if (wx < 0) wx = 0;
        if (wy > gradientArray.Length) wy = gradientArray.Length - 1;
        else if (wy < 0) wy = 0;
        if (wz > gradientArray.Length) wz = gradientArray.Length - 1;
        else if (wz < 0) wz = 0;

        return FBM_Seamless(wx, wy, wz, octaves, gain, amplitude, array_period_x, array_period_y, array_period_z);
    }
}