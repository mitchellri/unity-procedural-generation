using System.Collections.Generic;
using UnityEngine;

class TerrainGenerator : Generator
{
    // Public members
    public float[,] HeightMap { get; private set; }
    public float[,] WetnessMap { get; private set; }
    public float MinHeight { get; private set; }
    public float MaxHeight { get; private set; }
    public float AbsorptionCapacity { get; private set; } = 0.015f; // change to map
    public float WaterGain { get; private set; } = 0.25f;
    // Private members
    private PerlinNoise terrainNoise;
    private float absorptionRate = 0.00085f; // change to map
    private float[,] erosionMap;

    // Constructors
    public TerrainGenerator(int width, int length) : base(width, length)
    {
        terrainNoise = new PerlinNoise(width, length);
        HeightMap = new float[width, length];
        erosionMap = new float[width, length];
        WetnessMap = new float[width, length];
    }

    /// <summary>
    /// Generates terrain to class attributes
    /// </summary>
    public void GenerateTerrain(float inverseFrequency, float lacunarity, float gain, float amplitude, int octaves, float scale, int periodX = 0, int periodY = 0)
    {
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;
        base.Reset();
        Vector3 vectorIndex = new Vector3();
        for (int y = 0; y < Length; ++y)
        {
            vectorIndex.y = y;
            for (int x = 0; x < Width; ++x)
            {
                vectorIndex.x = x;
                if (periodX > 0 || periodY > 0) vectorIndex.z = terrainNoise.DW_Seamless(x, y, octaves, gain, amplitude, periodX, periodY) * scale;
                else vectorIndex.z = terrainNoise.DomainWarp(x, y, octaves, lacunarity, gain, amplitude, 1 / inverseFrequency) * scale;
                if (MinHeight > vectorIndex.z) MinHeight = vectorIndex.z;
                if (MaxHeight < vectorIndex.z) MaxHeight = vectorIndex.z;
                HeightMap[x, y] = vectorIndex.z;
                erosionMap[x, y] = 0;
                WetnessMap[x, y] = 0;
            }
        }
    }

    /// <summary>
    /// Applies a radial filter on existing terrain
    /// </summary>
    public void Radial(float radiusInner, float radiusOuter, float centerX, float centerY)
    {
        float min = MinHeight;
        MinHeight = float.MaxValue;
        float max = MaxHeight;
        MaxHeight = float.MinValue;
        base.Reset();
        Vector3 vectorIndex = new Vector3();
        for (int y = 0; y < Length; ++y)
        {
            vectorIndex.y = y;
            for (int x = 0; x < Width; ++x)
            {
                vectorIndex.x = x;
                vectorIndex.z = Mathf.Abs(HeightMap[x, y]);
                vectorIndex.z = radial(radiusInner, radiusOuter, centerX, centerY, vectorIndex);
                if (MinHeight > vectorIndex.z) MinHeight = vectorIndex.z;
                if (MaxHeight < vectorIndex.z) MaxHeight = vectorIndex.z;
                HeightMap[x, y] = vectorIndex.z;
                erosionMap[x, y] = 0;
                WetnessMap[x, y] = 0;
            }
        }
        // Height difference ratio
        max = (max - min) / (MaxHeight - MinHeight);
        MinHeight *= max;
        MaxHeight *= max;
        for (int y = 0; y < Length; ++y)
        {
            vectorIndex.y = y;
            for (int x = 0; x < Width; ++x)
            {
                vectorIndex.x = x;
                HeightMap[x, y] *= max;
                vectorIndex.z = HeightMap[x, y];
            }
        }
    }

    private float radial(float r1, float r2, float cx, float cy, Vector3 vector)
    {
        float ret = 1 - (Mathf.Sqrt(Mathf.Pow((cx - vector.x) / r1, 2) + Mathf.Pow((cy - vector.y) / r1, 2)));
        if (r1 > 0 && ret > 0) ret = Mathf.Max(-1, -ret) * 2;
        else ret = 0;
        ret += Mathf.Max(-1, (1 - Mathf.Sqrt(Mathf.Pow((cx - vector.x) / r2, 2) + Mathf.Pow((cy - vector.y) / r2, 2))));
        return vector.z * ret;
    }

    /// <summary>
    /// Simulates erosion on terrain
    /// </summary>
    public void DropletErosion(float directionInertia, float sedimentDeposit, float minSlope, float sedimentCapacity,
        float depositionSpeed, float erosionSpeed, float evaporationSpeed)
    {
        // Constants
        const float gravityX2 = 20 * 2;
        int[] direction = new int[2] { -1, 1 };

        // Variables
        int vectorX, vectorY,
            previousX, previousY,
            nextXInt, nextYInt;
        float directionX = 0, directionY = 0,
            vectorZ,
            vectorZ00, vectorZ10, vectorZ01, vectorZ11,
            nextXPrevious, nextYPrevious,
            nextZ00, nextZ10, nextZ01, nextZ11,
            gradientX, gradientY,
            nextXFloat, nextYFloat,
            nextZ,
            sediment, speed, water,
            zDifference,
            sedimentCount,
            vectorXFloat, vectorYFloat,
            totalAbsorbtionRate;

        uint iterations = (uint)(Length * Width),
            maxMoves = (uint)Length;
        for (int iteration = 0; iteration < iterations; ++iteration)
        {
            // Random location
            vectorX = Random.Range(0, Width);
            vectorY = Random.Range(0, Length);
            vectorZ = HeightMap[vectorX, vectorY];
            previousX = vectorX;
            previousY = vectorY;
            vectorXFloat = 0;
            vectorYFloat = 0;
            sediment = 0;
            speed = 0;
            water = 1;
            totalAbsorbtionRate = 0;

            // Neighbour vectorZ values
            if (vectorX + 1 >= Width || vectorY + 1 >= Length)
            {
                --iteration;
                continue;
            }
            vectorZ00 = vectorZ;
            vectorZ10 = HeightMap[vectorX + 1, vectorY];
            vectorZ01 = HeightMap[vectorX, vectorY + 1];
            vectorZ11 = HeightMap[vectorX + 1, vectorY + 1];

            // Interpolated gradient
            for (uint numMoves = 0; numMoves < maxMoves; ++numMoves)
            {
                if (WetnessMap[vectorX, vectorY] < AbsorptionCapacity)
                {
                    WetnessMap[vectorX, vectorY] += water * absorptionRate / 4;
                    totalAbsorbtionRate += absorptionRate / 4;
                }
                else
                {
                    float excess = (WetnessMap[vectorX, vectorY] - AbsorptionCapacity);
                    float transfer = excess * (water - WetnessMap[vectorX, vectorY]) * WaterGain;
                    water += transfer;
                    WetnessMap[vectorX, vectorY] -= transfer;
                }
                if (WetnessMap[vectorX + 1, vectorY] < AbsorptionCapacity)
                {
                    WetnessMap[vectorX + 1, vectorY] += water * absorptionRate / 4;
                    totalAbsorbtionRate += absorptionRate / 4;
                }
                if (WetnessMap[vectorX, vectorY + 1] < AbsorptionCapacity)
                {
                    WetnessMap[vectorX, vectorY + 1] += water * absorptionRate / 4;
                    totalAbsorbtionRate += absorptionRate / 4;
                }
                if (WetnessMap[vectorX + 1, vectorY + 1] < AbsorptionCapacity)
                {
                    WetnessMap[vectorX + 1, vectorY + 1] += water * absorptionRate / 4;
                    totalAbsorbtionRate += absorptionRate / 4;
                }
                water *= 1 - totalAbsorbtionRate;

                gradientX = vectorZ00 + vectorZ01 - vectorZ10 - vectorZ11;
                gradientY = vectorZ00 + vectorZ10 - vectorZ01 - vectorZ11;
                // Next position
                directionX = (directionX - gradientX) * directionInertia + gradientX;
                directionY = (directionY - gradientY) * directionInertia + gradientY;

                float magnitude = Mathf.Sqrt(directionX * directionX + directionY * directionY);
                if (magnitude <= Mathf.Epsilon)
                {
                    // Pick random direction
                    directionX = direction[Random.Range(0, 2)];
                    directionY = direction[Random.Range(0, 2)];
                }
                else
                {
                    directionX /= magnitude;
                    directionY /= magnitude;
                }

                nextXPrevious = previousX + directionX;
                nextYPrevious = previousY + directionY;

                // Sample next height
                nextXInt = Mathf.FloorToInt(nextXPrevious);
                nextYInt = Mathf.FloorToInt(nextYPrevious);
                nextXFloat = nextXPrevious - nextXInt;
                nextYFloat = nextYPrevious - nextYInt;
                if (nextXFloat < 0) nextXFloat = 0;
                if (nextYFloat < 0) nextYFloat = 0;
                // Neighbour of new point
                if (nextXInt < 0 || nextXInt + 1 >= Width || nextYInt < 0 || nextYInt + 1 >= Length)
                    break;
                nextZ00 = HeightMap[nextXInt, nextYInt];
                nextZ10 = HeightMap[nextXInt + 1, nextYInt];
                nextZ01 = HeightMap[nextXInt, nextYInt + 1];
                nextZ11 = HeightMap[nextXInt + 1, nextYInt + 1];

                // New height
                nextZ = (nextZ00 * (1 - nextXFloat) + nextZ10 * nextXFloat) * (1 - nextYFloat) + (nextZ01 * (1 - nextXFloat) + nextZ11 * nextXFloat) * nextYFloat;
                // If higher than current, try to deposit sediment up to neighbour height
                if (nextZ >= vectorZ)
                {
                    sedimentCount = (nextZ - vectorZ) + sedimentDeposit;
                    if (sedimentCount >= sediment)
                    {
                        // Deposit all sediment and stop
                        sedimentCount = sediment;
                        vectorZ = deposit(vectorZ, sedimentCount, vectorX, vectorY, vectorXFloat, vectorYFloat);
                        sediment = 0;
                        break;
                    }
                    vectorZ = deposit(vectorZ, sedimentCount, vectorX, vectorY, vectorXFloat, vectorYFloat);
                    sediment -= sedimentCount;
                    speed = 0;
                }

                // Transport capacity
                zDifference = vectorZ - nextZ;

                // deposit/erode (don't erode more than zDifference)
                sedimentCount = sediment - Mathf.Max(zDifference, minSlope) * speed * water * sedimentCapacity;
                if (sedimentCount >= 0)
                {
                    sedimentCount *= depositionSpeed;
                    if (vectorXFloat < 0 || vectorXFloat > 1) Debug.LogError(vectorXFloat + " vectorXFloat not between 0 and 1");
                    if (vectorYFloat < 0 || vectorYFloat > 1) Debug.LogError(vectorYFloat + " vectorYFloat not between 0 and 1");
                    zDifference = deposit(zDifference, sedimentCount, vectorX, vectorY, vectorXFloat, vectorYFloat);
                    sediment -= sedimentCount;
                }
                else
                {
                    // erode
                    sedimentCount *= -erosionSpeed;
                    sedimentCount = Mathf.Min(sedimentCount, zDifference * 0.99f);
                    float wi;
                    // Eroding the edge of the map results in pits
                    for (int yi = vectorY - 1; yi <= vectorY + 2; ++yi)
                    {
                        if (yi < 1 || yi >= Length - 1) continue;
                        float zo = yi - previousY;
                        float zo2 = zo * zo;

                        for (int xi = vectorX - 1; xi <= vectorX + 2; ++xi)
                        {
                            if (xi < 1 || xi >= Width - 1) continue;
                            float xo = xi - previousX;
                            wi = 1 - (xo * xo + zo2) * 0.25f;
                            if (wi <= 0) continue;
                            wi *= 0.1591549430918953f;
                            erode(sedimentCount, xi, yi, wi);
                        }
                    }
                    zDifference -= sedimentCount;
                    sediment += sedimentCount;
                }

                // move to the neighbour
                speed = Mathf.Sqrt(speed * speed + gravityX2 * zDifference);
                water *= 1 - evaporationSpeed;

                previousX = Mathf.RoundToInt(nextXPrevious); previousY = Mathf.RoundToInt(nextYPrevious);
                vectorX = nextXInt; vectorY = nextYInt;
                vectorXFloat = nextXFloat; vectorYFloat = nextYFloat;

                vectorZ = nextZ;
                vectorZ00 = nextZ00;
                vectorZ10 = nextZ10;
                vectorZ01 = nextZ01;
                vectorZ11 = nextZ11;
            }
        }
    }

    /// <summary>
    /// Simluates a droplet that erodes terrain
    /// </summary>
    /// <param name="node">Source node for droplet</param>
    /// <param name="destinationLevel">Droplet will stop at this z-level</param>
    /// <param name="water">Amount of water in droplet</param>
    /// Note: Utilizing this in DropletErosion heavily impacts performance
    public void Droplet(int x, int y, float destinationLevel, float directionInertia, float sedimentDeposit, float minSlope, float sedimentCapacity,
        float depositionSpeed, float erosionSpeed, float evaporationSpeed, float water = 1)
    {
        const float gravityX2 = 20 * 2;
        uint maxMoves = (uint)Length / 4;
        // Variables
        int[] direction = new int[2] { -1, 1 };
        float[,] precipitationMap = new float[Width, Length];
        int previousX, previousY,
            nextXInt, nextYInt;
        float directionX = 0, directionY = 0,
            vectorZ,
            vectorZ00, vectorZ10, vectorZ01, vectorZ11,
            nextXPrevious, nextYPrevious,
            nextZ00, nextZ10, nextZ01, nextZ11,
            gradientX, gradientY,
            nextXFloat, nextYFloat,
            nextZ,
            sediment, speed,
            zDifference = 0,
            sedimentCount,
            vectorXFloat, vectorYFloat,
            totalAbsorbtionRate = 0;

        if (x + 1 >= Width || y + 1 >= Length) return;
        vectorZ = HeightMap[x, y];
        previousX = x;
        previousY = y;
        vectorXFloat = 0;
        vectorYFloat = 0;
        sediment = 0;
        speed = 0;
        vectorZ00 = vectorZ;
        vectorZ10 = HeightMap[x + 1, y];
        vectorZ01 = HeightMap[x, y + 1];
        vectorZ11 = HeightMap[x + 1, y + 1];
        maxMoves = (uint)Length / 4;
        uint numMoves;
        for (numMoves = 0; vectorZ > destinationLevel && numMoves < maxMoves; ++numMoves)
        {
            // Absorb water
            if (WetnessMap[x, y] < AbsorptionCapacity)
            {
                WetnessMap[x, y] += water * absorptionRate / 4;
                totalAbsorbtionRate += absorptionRate / 4;
            }
            else // Carry excess water
            {
                float excess = (WetnessMap[x, y] - AbsorptionCapacity);
                float transfer = excess * (water - WetnessMap[x, y]) * WaterGain;
                water += transfer;
                WetnessMap[x, y] -= transfer;
            }
            if (WetnessMap[x + 1, y] < AbsorptionCapacity)
            {
                WetnessMap[x + 1, y] += water * absorptionRate / 4;
                totalAbsorbtionRate += absorptionRate / 4;
            }
            if (WetnessMap[x, y + 1] < AbsorptionCapacity)
            {
                WetnessMap[x, y + 1] += water * absorptionRate / 4;
                totalAbsorbtionRate += absorptionRate / 4;
            }
            if (WetnessMap[x + 1, y + 1] < AbsorptionCapacity)
            {
                WetnessMap[x + 1, y + 1] += water * absorptionRate / 4;
                totalAbsorbtionRate += absorptionRate / 4;
            }
            water *= 1 - totalAbsorbtionRate;

            gradientX = vectorZ00 + vectorZ01 - vectorZ10 - vectorZ11;
            gradientY = vectorZ00 + vectorZ10 - vectorZ01 - vectorZ11;
            // Next position
            directionX = (directionX - gradientX) * directionInertia + gradientX;
            directionY = (directionY - gradientY) * directionInertia + gradientY;

            float magnitude = Mathf.Sqrt(directionX * directionX + directionY * directionY);
            if (magnitude <= Mathf.Epsilon)
            {
                // Pick random direction
                directionX = direction[Random.Range(0, 2)];
                directionY = direction[Random.Range(0, 2)];
            }
            else
            {
                directionX /= magnitude;
                directionY /= magnitude;
            }

            nextXPrevious = previousX + directionX;
            nextYPrevious = previousY + directionY;

            // Sample next height
            nextXInt = Mathf.FloorToInt(nextXPrevious);
            nextYInt = Mathf.FloorToInt(nextYPrevious);
            nextXFloat = nextXPrevious - nextXInt;
            nextYFloat = nextYPrevious - nextYInt;
            if (nextXFloat < 0) nextXFloat = 0;
            if (nextYFloat < 0) nextYFloat = 0;
            // Neighbour of new point
            if (nextXInt < 0 || nextXInt + 1 >= Width || nextYInt < 0 || nextYInt + 1 >= Length)
                break;
            nextZ00 = HeightMap[nextXInt, nextYInt];
            nextZ10 = HeightMap[nextXInt + 1, nextYInt];
            nextZ01 = HeightMap[nextXInt, nextYInt + 1];
            nextZ11 = HeightMap[nextXInt + 1, nextYInt + 1];

            // New height
            nextZ = (nextZ00 * (1 - nextXFloat) + nextZ10 * nextXFloat) * (1 - nextYFloat) + (nextZ01 * (1 - nextXFloat) + nextZ11 * nextXFloat) * nextYFloat;
            // If higher than current, try to deposit sediment up to neighbour height
            if (nextZ >= vectorZ)
            {
                sedimentCount = (nextZ - vectorZ) + sedimentDeposit;
                if (sedimentCount >= sediment)
                {
                    // Deposit all sediment and stop
                    sedimentCount = sediment;
                    vectorZ = deposit(vectorZ, sedimentCount, x, y, vectorXFloat, vectorYFloat);
                    sediment = 0;
                    break;
                }
                vectorZ = deposit(vectorZ, sedimentCount, x, y, vectorXFloat, vectorYFloat);
                sediment -= sedimentCount;
                speed = 0;
            }

            // Transport capacity
            zDifference = vectorZ - nextZ;

            // deposit/erode (don't erode more than zDifference)
            sedimentCount = sediment - Mathf.Max(zDifference, minSlope) * speed * water * sedimentCapacity;
            if (sedimentCount >= 0)
            {
                sedimentCount *= depositionSpeed;
                if (vectorXFloat < 0 || vectorXFloat > 1) Debug.LogError(vectorXFloat + " vectorXFloat not between 0 and 1");
                if (vectorYFloat < 0 || vectorYFloat > 1) Debug.LogError(vectorYFloat + " vectorYFloat not between 0 and 1");
                zDifference = deposit(zDifference, sedimentCount, x, y, vectorXFloat, vectorYFloat);
                sediment -= sedimentCount;
            }
            else
            {
                // erode
                sedimentCount *= -erosionSpeed;
                sedimentCount = Mathf.Min(sedimentCount, zDifference * 0.99f);
                float wi;
                for (int yi = y - 1; yi <= y + 2; ++yi)
                {
                    if (yi < 1 || yi >= Length - 1) continue;
                    float zo = yi - previousY;
                    float zo2 = zo * zo;

                    for (int xi = x - 1; xi <= x + 2; ++xi)
                    {
                        if (xi < 1 || xi >= Width - 1) continue;
                        float xo = xi - previousX;
                        wi = 1 - (xo * xo + zo2) * 0.25f;
                        if (wi <= 0) continue;
                        wi *= 0.1591549430918953f;
                        erode(sedimentCount, xi, yi, wi);
                    }
                }
                zDifference -= sedimentCount;
                sediment += sedimentCount;
            }

            // move to the neighbour
            speed = Mathf.Sqrt(speed * speed + gravityX2 * zDifference);
            water *= 1 - evaporationSpeed;

            previousX = Mathf.RoundToInt(nextXPrevious); previousY = Mathf.RoundToInt(nextYPrevious);
            x = nextXInt; y = nextYInt;
            vectorXFloat = nextXFloat; vectorYFloat = nextYFloat;

            vectorZ = nextZ;
            vectorZ00 = nextZ00;
            vectorZ10 = nextZ10;
            vectorZ01 = nextZ01;
            vectorZ11 = nextZ11;
        }
    }

    private void depositAt(int x, int y, float w, float ds)
    {
        float delta = w * ds;
        erosionMap[x, y] += delta;
        HeightMap[x, y] += delta;
    }

    private float deposit(float z, float ds, int xi, int yi, float xf, float yf)
    {
        depositAt(xi, yi, (1 - xf) * (1 - yf), ds);
        if (xi + 1 < Width) depositAt(xi + 1, yi, xf * (1 - yf), ds);
        if (yi + 1 < Length) depositAt(xi, yi + 1, (1 - xf) * yf, ds);
        if (xi + 1 < Width && yi + 1 < Length) depositAt(xi + 1, yi + 1, xf * yf, ds);
        return z + ds;
    }

    private void erode(float ds, int x, int y, float w)
    {
        float delta = ds * w;
        float temp = HeightMap[x, y] -= delta;
        float r = x;
        float d = erosionMap[x, y];
        if (delta <= d) d -= delta;
        else
        {
            r += delta - d;
            d = 0;
            x = Mathf.RoundToInt(r);
            if (x < 0 || x >= Width) return;
        }
        erosionMap[x, y] = d;
    }

    /// <summary>
    /// Resets noise
    /// </summary>
    public override void Reset()
    {
        // Base reset not required, is reset on generation
        terrainNoise.ResetGradientArray();
        // Map resets not required, it is all overwritten
    }

    private int costFunction(Vector3 movementVector)
    {
        return Mathf.RoundToInt(movementVector.z > 0 ? 999 : movementVector.z + Mathf.Abs(movementVector.y) + Mathf.Abs(movementVector.x));
    }
}
