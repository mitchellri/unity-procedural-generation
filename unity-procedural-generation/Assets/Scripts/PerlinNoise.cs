﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Added
using UnityEngine.Tilemaps;

public class PerlinNoise
{
    // Private members
    private static readonly int gradientRange = 100;
    private Vector3[,] gradientArray;

    public PerlinNoise(int width, int height)
    {
        initialize(width, height);
    }
    public PerlinNoise(int width, int height, int seed)
    {
        Random.InitState(seed);
        initialize(width, height);
    }
    private void initialize(int width, int height)
    {
        gradientArray = getGradientArray(width, height);
    }
    static private Vector2 getGradientVector()
    {
        return new Vector2(
            Random.Range(-gradientRange, gradientRange),
            Random.Range(-gradientRange, gradientRange))
            / gradientRange;
    }
    static private Vector3[,] getGradientArray(int width, int height)
    {
        Vector3[,] gradientArray = new Vector3[width, height];
        for (int i = 0; i < width; ++i)
            for (int j = 0; j < height; ++j)
                gradientArray[i, j] = getGradientVector();
        return gradientArray;
    }
    /* Function to linearly interpolate between a0 and a1
     * Weight w should be in the range [0.0, 1.0]
     *
     * as an alternative, this slightly faster equivalent function (macro) can be used:
     * #define lerp(a0, a1, w) ((a0) + (w)*((a1) - (a0))) 
     */
    static private float lerp(float a0, float a1, float w)
    {
        return (1.0f - w) * a0 + w * a1;
    }
    // Computes the dot product of the distance and gradient vectors.
    private float dotArrayGradient(int ix, int iy, float x, float y)
    {
        // Compute the distance vector
        float dx = x - (float)ix;
        float dy = y - (float)iy;

        // Compute the dot-product
        return (dx * gradientArray[ix, iy].x + dy * gradientArray[ix, iy].y);
    }
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
        ix0 = lerp(n0, n1, sx);

        n0 = dotArrayGradient(x0, y1, x, y);
        n1 = dotArrayGradient(x1, y1, x, y);
        ix1 = lerp(n0, n1, sx);
        value = lerp(ix0, ix1, sy);
        return value;
    }
    public float FractionalBrownianMotion(float x, float y, int octaves = 8, float lacunarity = 2, float gain = (float)0.5)
    {
        float amplitude = 1;
        float frequency = 1;
        float sum = 0;
        for (int i = 0; i < octaves; ++i)
        {
            sum += amplitude * Perlin(x * frequency, y * frequency);
            amplitude *= gain;
            frequency *= lacunarity;
        }
        return sum;
    }
}