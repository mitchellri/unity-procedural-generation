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
    private PerlinNoise wormNoise;
    private float absorptionRate = 0.00085f; // change to map
    private float[,] erosionMap;

    // Constructors
    public TerrainGenerator(int width, int length, int height) : base(width, length, height)
    {
        terrainNoise = new PerlinNoise(width, length, height);
        wormNoise = new PerlinNoise(width, length, height);
        HeightMap = new float[width, length];
        erosionMap = new float[width, length];
        WetnessMap = new float[width, length];
    }

    /// <summary>
    /// Generates terrain to class attributes
    /// </summary>
    public void GenerateTerrain(float inverseFrequency, float lacunarity, float gain, float amplitude, int octaves, float scale, int periodX = 0, int periodY = 0, int periodZ = 0, int z = 0)
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
                // Terrain (heightmap) only, z level 0
                if (periodX > 0 || periodY > 0) vectorIndex.z = terrainNoise.DW_Seamless(x, y, z, octaves, gain, amplitude, periodX, periodY, periodZ) * scale;
                else vectorIndex.z = terrainNoise.DomainWarp(x, y, z, octaves, lacunarity, gain, amplitude, 1 / inverseFrequency) * scale;
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

    private void absorbAt(Vector3 vector, ref float water)
    {
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        float waterTransferCapacty = water * absorptionRate / 4,
            remainingCapacity, transferCapacity;
        for (int x = vectorInt.x; x < vectorInt.x + 2; ++x)
        {
            for (int y = vectorInt.y; y < vectorInt.y + 2; ++y)
            {
                if (WetnessMap[x, y] < AbsorptionCapacity)
                {
                    remainingCapacity = Mathf.Max(0, AbsorptionCapacity - WetnessMap[x, y]);
                    transferCapacity = Mathf.Min(waterTransferCapacty, remainingCapacity);
                    WetnessMap[x, y] += transferCapacity;
                    water -= transferCapacity;
                }
            }
        }
    }

    private float[,] getGradient(Vector3 vector)
    {
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        return new float[2, 2] {
            { vector.z, HeightMap[vectorInt.x, vectorInt.y + 1] },
            { HeightMap[vectorInt.x + 1, vectorInt.y], HeightMap[vectorInt.x + 1, vectorInt.y + 1] }
        };
    }

    private void setMovingDirection(float[,] gradient, float directionInertia, ref Vector3 direction)
    {
        int[] directions = new int[2] { -1, 1 };
        float gradientX = gradient[0, 0] + gradient[0, 1] - gradient[1, 0] - gradient[1, 1],
            gradientY = gradient[0, 0] + gradient[1, 0] - gradient[0, 1] - gradient[1, 1];
        direction.Set(
                (direction.x - gradientX) * directionInertia + gradientX,
                (direction.y - gradientY) * directionInertia + gradientY,
                0
            );

        float magnitude = Mathf.Sqrt(direction.x * direction.x + direction.y * direction.y + direction.z * direction.z);
        if (magnitude <= Mathf.Epsilon)
        {
            // Not moving - pick random direction
            direction.x = directions[Random.Range(0, 2)];
            direction.y = directions[Random.Range(0, 2)];
        }
        else
        {
            direction.x /= magnitude;
            direction.y /= magnitude;
            direction.z /= magnitude;
        }
    }

    /// <summary>
    /// Simulates erosion on terrain
    /// </summary>
    public void DropletErosion(float directionInertia, float sedimentDeposit, float minSlope, float sedimentCapacity,
        float depositionSpeed, float erosionSpeed, float evaporationSpeed)
    {
        // Constants
        const float gravityX2 = 20 * 2;

        // Variables
        Vector3 position = new Vector3(),
            positionRemainder = new Vector3(),
            nextPosition = new Vector3(),
            nextPositionRemainder = new Vector3(),
            direction = new Vector3(),
            transport = new Vector3();
        float[,] gradient = new float[2, 2];
        float sediment, sedimentCount, speed, water;

        uint dropletCount = (uint)(Length * Width),
            maxMoves = (uint)Length;
        for (int dropletIndex = 0; dropletIndex < dropletCount; ++dropletIndex)
        {
            // Droplet location
            position.Set(
                Random.Range(0, Width),
                Random.Range(0, Length),
                0
            );
            position.z = HeightMap[(int)position.x, (int)position.y];

            // Reset tracking parameters
            sediment = 0;
            speed = 0;
            water = 1;

            // Neighbour position.z values
            if (position.x + 1 >= Width || position.y + 1 >= Length)
            {
                --dropletIndex;
                continue;
            }
            gradient = getGradient(position);

            // Moving droplet
            for (uint numMoves = 0; numMoves < maxMoves; ++numMoves)
            {
                // Surrounding ground absorbs water
                absorbAt(position, ref water);

                // Droplet moves downhill with inertia
                setMovingDirection(gradient, directionInertia, ref direction);

                // Next position
                nextPosition = position + direction;
                if (Mathf.FloorToInt(nextPosition.x) < 0 || Mathf.FloorToInt(nextPosition.x) + 1 >= Width || Mathf.FloorToInt(nextPosition.y) < 0 || Mathf.FloorToInt(nextPosition.y) + 1 >= Length) break; // Stop droplet if off map

                // Gradient modifiers
                nextPositionRemainder.x = nextPosition.x % 1;
                nextPositionRemainder.y = nextPosition.y % 1;
                if (nextPositionRemainder.x < 0) nextPositionRemainder.x = 0;
                if (nextPositionRemainder.y < 0) nextPositionRemainder.y = 0;

                // Deposited height of new point
                gradient = getGradient(new Vector3(nextPosition.x, nextPosition.y, HeightMap[Mathf.FloorToInt(nextPosition.x), Mathf.FloorToInt(nextPosition.y)]));
                nextPosition.z = (gradient[0, 0] * (1 - nextPositionRemainder.x) + gradient[1, 0] * nextPositionRemainder.x) * (1 - nextPositionRemainder.y) + (gradient[0, 1] * (1 - nextPositionRemainder.x) + gradient[1, 1] * nextPositionRemainder.x) * nextPositionRemainder.y;

                // If higher than current, try to deposit sediment up to neighbour height
                if (nextPosition.z >= position.z)
                {
                    sedimentCount = (nextPosition.z - position.z) + sedimentDeposit;
                    if (sedimentCount >= sediment)
                    {
                        // Deposit all sediment and stop
                        sedimentCount = sediment;
                        deposit(sedimentCount, positionRemainder, ref position);
                        sediment = 0;
                        break;
                    }
                    deposit(sedimentCount, positionRemainder, ref position);
                    sediment -= sedimentCount;
                    speed = 0;
                }

                // Transport capacity
                transport.Set(
                    position.x,
                    position.y,
                    position.z - nextPosition.z
                    );
                sedimentCount = sediment - Mathf.Max(transport.z, minSlope) * speed * water * sedimentCapacity;

                // Deposit/erode
                if (sedimentCount >= 0)
                {
                    sedimentCount *= depositionSpeed;
                    deposit(sedimentCount, positionRemainder, ref transport);
                    sediment -= sedimentCount;
                }
                else
                {
                    // Don't erode more than transport.z
                    sedimentCount *= -erosionSpeed;
                    sedimentCount = Mathf.Min(sedimentCount, transport.z * 0.99f);
                    float wi;

                    // Eroding the edge of the map results in pits
                    for (int yi = (int)position.y - 1; yi < position.y + 2; ++yi)
                    {
                        if (yi < 1 || yi >= Length - 1) continue;
                        float zo = yi - position.y;
                        float zo2 = zo * zo;

                        for (int xi = (int)position.x - 1; xi < position.x + 2; ++xi)
                        {
                            if (xi < 1 || xi >= Width - 1) continue;
                            float xo = xi - position.x;
                            wi = 1 - (xo * xo + zo2) * 0.25f;
                            if (wi <= 0) continue;
                            wi *= 0.1591549430918953f;
                            erode(sedimentCount, wi, new Vector3(xi, yi, position.z));
                        }
                    }
                    transport.z -= sedimentCount;
                    sediment += sedimentCount;
                }

                // Update water
                speed = Mathf.Sqrt(speed * speed + gravityX2 * transport.z);
                water *= 1 - evaporationSpeed;

                // Move to neighbour
                position = nextPosition;
                positionRemainder = nextPositionRemainder;
            }
        }
    }

    private void depositAt(Vector3Int vector, float w, float deltaSediment)
    {
        float delta = w * deltaSediment;
        erosionMap[vector.x, vector.y] += delta;
        HeightMap[vector.x, vector.y] += delta;
    }

    private void deposit(float deltaSediment, Vector3 vectorRemainder, ref Vector3 vector)
    {
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        depositAt(vectorInt, (1 - vectorRemainder.x) * (1 - vectorRemainder.y), deltaSediment);
        if (vectorInt.x + 1 < Width) depositAt(vectorInt + new Vector3Int(1, 0, 0), vectorRemainder.x * (1 - vectorRemainder.y), deltaSediment);
        if (vectorInt.y + 1 < Length) depositAt(vectorInt + new Vector3Int(0, 1, 0), (1 - vectorRemainder.x) * vectorRemainder.y, deltaSediment);
        if (vectorInt.x + 1 < Width && vectorInt.y + 1 < Length) depositAt(vectorInt + new Vector3Int(1, 1, 0), vectorRemainder.x * vectorRemainder.y, deltaSediment);
        vector.z += deltaSediment;
    }

    private void erode(float ds, float w, Vector3 vector)
    {
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        float delta = ds * w;
        float temp = HeightMap[vectorInt.x, vectorInt.y] -= delta;
        float r = vector.x;
        float d = erosionMap[vectorInt.x, vectorInt.y];
        if (delta <= d) d -= delta;
        else
        {
            r += delta - d;
            d = 0;
            vector.x = Mathf.RoundToInt(r);
            if (vector.x < 0 || vector.x >= Width) return;
        }
        erosionMap[vectorInt.x, vectorInt.y] = d;
    }

    /// Note: http://libnoise.sourceforge.net/examples/worms/index.html
    public void GenerateCave(int x, int y, int z, int caveLength, float twistiness, int radius, float inverseFrequency, float lacunarity, float gain, float amplitude, int octaves, float scale, float radiusVarianceScale = 0)
    {
        float speed = radius - 1;
        if (speed < 1) speed = 1;
        float lateralSpeed = 10f;
        Vector3 speedMod = new Vector3(1, 1, 0.5f);
        Vector3 lateralSpeedMod = new Vector3(1, 1, 2);
        Vector3 headOffset = new Vector3();

        float noiseValue;
        float variedRadius;
        int variedRadiusInt = 0;
        Vector3 radiusVarianceOffset = new Vector3(
            Random.Range(0, Width),
            Random.Range(0, Length),
            Random.Range(0, Height)
            );

        Vector3 headNoisePos = new Vector3(x, y, z - 10);
        Vector3 headScreenPos = new Vector3(x, y, z - 10);
        Vector2Int headScreenPosInt = new Vector2Int();

        for (int curSegment = 0; curSegment < caveLength; ++curSegment)
        {
            // The angle of the head segment is used to determine the direction the worm
            // moves.  The worm moves in the opposite direction.
            noiseValue = wormNoise.DomainWarp(headNoisePos.x + curSegment * twistiness, headNoisePos.y, headNoisePos.z, octaves, lacunarity, gain, amplitude, 1 / inverseFrequency);
            headOffset.Set(
                    Mathf.Cos(noiseValue * 2 * Mathf.PI) * speed,
                    Mathf.Sin(noiseValue * 2 * Mathf.PI) * speed,
                    Mathf.Atan(noiseValue * 2 * Mathf.PI) * speed
                );
            headScreenPos -= Vector3.Scale(headOffset, speedMod);

            // Slightly update the coordinates of the input value, in "noise space".
            // This causes the worm's shape to be slightly different in the next frame.
            // The x coordinate of the input value is shifted in a negative direction,
            // which propagates the previous Perlin-noise values over to subsequent
            // segments.  This produces a "slithering" effect.
            headOffset.Set(
                    -speed * 2,
                    lateralSpeed,
                    lateralSpeed
                );
            headNoisePos += Vector3.Scale(headOffset, lateralSpeedMod);

            // Make sure the worm's head is within the window, otherwise the worm may
            // escape.  Horrible, horrible freedom!
            if (headScreenPos.x > Width || headScreenPos.x < 0) continue;
            if (headScreenPos.y > Length || headScreenPos.y < 0) continue;
            headScreenPos.x = Mathf.Clamp(headScreenPos.x, 0, Width - 1);
            headScreenPos.y = Mathf.Clamp(headScreenPos.y, 0, Length - 1);

            headScreenPosInt.Set(
                (int)headScreenPos.x,
                (int)headScreenPos.y
                );

            variedRadius = radius;

            if (radiusVarianceScale != 0)
            {
                noiseValue = wormNoise.DomainWarp((headNoisePos.x + radiusVarianceOffset.x) + curSegment * twistiness, headNoisePos.y + radiusVarianceOffset.y, headNoisePos.z + radiusVarianceOffset.z, octaves, lacunarity, gain, amplitude, 1 / inverseFrequency);
                if (variedRadius == 0)
                {
                    variedRadius = (0.5f - noiseValue) * radiusVarianceScale;
                }
                else
                {
                    variedRadius += radius * (0.5f - noiseValue) * radiusVarianceScale;
                }
            }

            // Remove land at head of worm
            if (variedRadius <= 0)
            {
                if (headScreenPos.z > HeightMap[headScreenPosInt.x, headScreenPosInt.y]) continue;
                HeightMap[headScreenPosInt.x, headScreenPosInt.y] = headScreenPos.z;
            }
            else
            {
                if (variedRadius < 1)
                {
                    if (headScreenPos.z < HeightMap[headScreenPosInt.x, headScreenPosInt.y])
                        HeightMap[headScreenPosInt.x, headScreenPosInt.y] = headScreenPos.z;
                    variedRadiusInt = 0;
                }
                // Thiccness of worm
                else
                {
                    variedRadiusInt = (int)variedRadius;
                    for (int i = -variedRadiusInt; i < variedRadiusInt; ++i)
                    {
                        if (headScreenPosInt.x + i >= Width || headScreenPosInt.x + i < 0) continue;
                        for (int j = -variedRadiusInt; j < variedRadiusInt; ++j)
                        {
                            if (headScreenPosInt.y + j >= Length || headScreenPosInt.y + j < 0) continue;
                            for (int k = -variedRadiusInt; k < variedRadiusInt; ++k)
                            {
                                if (i * i + j * j + k * k <= variedRadius * variedRadius
                                    && headScreenPos.z + k < HeightMap[headScreenPosInt.x + i, headScreenPosInt.y + j])
                                {
                                    HeightMap[headScreenPosInt.x + i, headScreenPosInt.y + j] = headScreenPos.z + k;
                                }
                            }
                        }
                    }
                }

                // Move to end of radius
                speed = (radius - variedRadiusInt) - 1;
                if (speed < 1)
                {
                    speed = 1;
                }
            }

            if (HeightMap[headScreenPosInt.x, headScreenPosInt.y] < MinHeight)
                MinHeight = HeightMap[headScreenPosInt.x, headScreenPosInt.y];
        }
    }

    /// <summary>
    /// Resets noise
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        terrainNoise.ResetGradientArray();
        wormNoise.ResetGradientArray();
        HeightMap = new float[Width, Length];
    }

    private int costFunction(Vector3 movementVector)
    {
        return Mathf.RoundToInt(movementVector.z > 0 ? 999 : movementVector.z + Mathf.Abs(movementVector.y) + Mathf.Abs(movementVector.x));
    }
}
