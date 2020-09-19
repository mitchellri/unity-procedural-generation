using UnityEngine;

class TerrainGenerator : Generator
{
    // Public members
    public float[,,] WetnessMap { get; private set; }
    public float RangeHeight { get; private set; }
    public float AbsorptionCapacity { get; private set; } = 0.015f;
    public float WaterGain { get; private set; } = 0.25f;

    // Public parameters
    public new int Width
    {
        get
        {
            return width;
        }
        set
        {
            if (Width != value)
            {
                width = value;
                if (Width > 0 && Length > 0)
                {
                    heightMapNoise = new PerlinNoise(Width, Length, 1);
                    if (Height > 0)
                    {
                        caveNoise = new PerlinNoise(Width, Length, Height);
                    }
                }
                else
                {
                    caveNoise = null;
                    heightMapNoise = null;
                }
            }
        }
    }
    public override int Length
    {
        get
        {
            return length;
        }
        set
        {
            if (Length != value)
            {
                length = value;
                if (Width > 0 && Length > 0)
                {
                    heightMapNoise = new PerlinNoise(Width, Length, 1);
                    if (Height > 0)
                    {
                        caveNoise = new PerlinNoise(Width, Length, Height);
                    }
                }
                else
                {
                    caveNoise = null;
                    heightMapNoise = null;
                }
            }
        }
    }
    public override int Height
    {
        get
        {
            return height;
        }
        set
        {
            if (Height != value)
            {
                height = value;
                if (Width > 0 && Length > 0 && Height > 0)
                {
                    caveNoise = new PerlinNoise(Width, Length, Height);
                }
                else
                {
                    caveNoise = null;
                }
            }
        }
    }
    /// <summary>
    /// Height below generated terrain
    /// </summary>
    public int UndergroundHeight;
    /// <summary>
    /// Height above generated terrain
    /// </summary>
    public int SkyHeight;
    // Noise parameters
    public float InverseFrequency;
    public float Lacunarity;
    public float Gain;
    public float Amplitude;
    public float Scale;
    public int Octaves;
    // Seamless parameters
    public Vector3Int Period;
    // Radial parameters
    public float RadiusInner;
    public float RadiusOuter;
    public Vector2Int RadiusCenter;
    // Cave parameters
    public float CaveRadiusVarianceScale;
    public float CaveRadius;
    public float CaveTwistiness;
    public float CaveSegmentLength;
    // Erosion parameters
    public float DirectionInertia;
    public float SedimentDeposit;
    public float MinSlope;
    public float SedimentCapacity;
    public float DepositionSpeed;
    public float ErosionSpeed;
    public float EvaporationSpeed;

    // Private members
    private int width;
    private int length;
    private int height;
    private float[,,] erosionMap;
    private const float absorptionRate = 0.00085f;
    // Noise members
    private PerlinNoise heightMapNoise;
    private PerlinNoise caveNoise;

    public TerrainGenerator()
    {
        Period = new Vector3Int();
    }

    /// <summary>
    /// Generates terrain to class attributes
    /// </summary>
    public void GenerateTerrain(bool seamless = false, bool radial = false)
    {
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;

        float[,] heightMap = new float[Width, Length];
        RangeHeight = applyHeightMap(ref heightMap, radial, seamless);
        MinHeight = UndergroundHeight;
        MaxHeight = UndergroundHeight + RangeHeight;
        Height = UndergroundHeight + Mathf.CeilToInt(RangeHeight + 1) + SkyHeight;
        WorldMap = new float[Width, Length, Height];
        WetnessMap = new float[Width, Length, Height];
        erosionMap = new float[Width, Length, Height];

        float z;
        int zInt;
        for (int y = 0; y < Length; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                z = heightMap[x, y] + UndergroundHeight;
                zInt = (int)z;
                WorldMap[x, y, zInt] = z - zInt;

                for (--zInt; zInt >= 0; --zInt)
                {
                    WorldMap[x, y, zInt] = 1;
                }
            }
        }
    }

    /// <returns>Total height of new heightMap</returns>
    private float applyHeightMap(ref float[,] heightMap, bool radial, bool seamless)
    {
        float height = 0;
        float minHeight = float.MaxValue,
            maxHeight = float.MinValue;
        for (int x = 0; x < heightMap.GetLength(0); ++x)
        {
            for (int y = 0; y < heightMap.GetLength(1); ++y)
            {
                if (seamless) heightMap[x, y] = heightMapNoise.DW_Seamless(x, y, 0, Octaves, Gain, Amplitude, Period.x, Period.y, Period.z) * Scale;
                else heightMap[x, y] = heightMapNoise.DomainWarp(x, y, 0, Octaves, Lacunarity, Gain, Amplitude, 1 / InverseFrequency) * Scale;
                if (minHeight > heightMap[x, y]) minHeight = heightMap[x, y];
                if (maxHeight < heightMap[x, y]) maxHeight = heightMap[x, y];
            }
        }

        if (radial)
        {
            applyRadial(ref heightMap, ref minHeight, ref maxHeight);
        }

        height = maxHeight - minHeight;

        for (int x = 0; x < heightMap.GetLength(0); ++x)
        {
            for (int y = 0; y < heightMap.GetLength(1); ++y)
            {
                heightMap[x, y] = Mathf.Max(float.Epsilon, heightMap[x, y] - minHeight); // Always floor at 0
            }
        }

        return height;
    }

    /// <summary>
    /// Applies a radial filter on a heightMap
    /// </summary>
    /// <param name="minHeight">Mimimum height of heightMap being passed</param>
    /// <param name="maxHeight">Maximum height of heightMap being passed</param>
    /// <returns>Total height of new heightMap</returns>
    private float applyRadial(ref float[,] heightMap, ref float minHeight, ref float maxHeight)
    {
        float newMinHeight = float.MaxValue,
            newMaxHeight = float.MinValue;
        Vector3 vector = new Vector3();
        for (int x = 0; x < heightMap.GetLength(0); ++x)
        {
            vector.x = x;
            for (int y = 0; y < heightMap.GetLength(1); ++y)
            {
                vector.y = y;
                vector.z = heightMap[x, y] - minHeight;
                heightMap[x, y] = getRadialHeightAt(vector);
                if (newMinHeight > heightMap[x, y]) newMinHeight = heightMap[x, y];
                if (newMaxHeight < heightMap[x, y]) newMaxHeight = heightMap[x, y];
            }
        }

        minHeight = newMinHeight;
        maxHeight = newMaxHeight;
        return newMaxHeight - newMinHeight;
    }

    private float getRadialHeightAt(Vector3 vector)
    {
        // Center is 0
        float innerFilter = 0;
        if (RadiusInner > 0)
        {
            innerFilter = Mathf.Sqrt(Mathf.Pow((RadiusCenter.x - vector.x) / RadiusInner, 2) + Mathf.Pow((RadiusCenter.y - vector.y) / RadiusInner, 2));
            innerFilter = Mathf.Clamp(innerFilter, 0, 1);
        }
        // Center is 1
        float outerFilter = 0;
        if (RadiusOuter > 0)
        {
            outerFilter = 1 - Mathf.Sqrt(Mathf.Pow((RadiusCenter.x - vector.x) / RadiusOuter, 2) + Mathf.Pow((RadiusCenter.y - vector.y) / RadiusOuter, 2));
            outerFilter = Mathf.Clamp(outerFilter, 0, 1);
        }
        return vector.z * (innerFilter * outerFilter);
    }

    private void absorbAt(Vector3 vector, ref float water)
    {
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        if (vectorInt.z == Height)
        {
            --vectorInt.z; // Top of the map, out of bounds of array
        }
        float waterTransferCapacty = water * absorptionRate / 4,
            remainingCapacity, transferCapacity,
            tileAbsorptionCapacity;
        for (int x = vectorInt.x; x < vectorInt.x + 2; ++x)
        {
            for (int y = vectorInt.y; y < vectorInt.y + 2; ++y)
            {
                //tileAbsorptionCapacity = WorldMap[x, y, vectorInt.z] * AbsorptionCapacity;
                tileAbsorptionCapacity = AbsorptionCapacity;
                if (tileAbsorptionCapacity > 0 && WetnessMap[x, y, vectorInt.z] < tileAbsorptionCapacity)
                {
                    remainingCapacity = Mathf.Max(0, tileAbsorptionCapacity - WetnessMap[x, y, vectorInt.z]);
                    transferCapacity = Mathf.Min(waterTransferCapacty, remainingCapacity);
                    WetnessMap[x, y, vectorInt.z] += transferCapacity;
                    water -= transferCapacity;
                }
                else if (WetnessMap[x, y, vectorInt.z] > tileAbsorptionCapacity)
                {
                    water += WetnessMap[x, y, vectorInt.z] - tileAbsorptionCapacity;
                    WetnessMap[x, y, vectorInt.z] = tileAbsorptionCapacity;
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
            { GetFloorAt(vector),                            GetFloorAt(vector + new Vector3Int(0, 1, 0)) },
            { GetFloorAt(vector + new Vector3Int(1, 0, 0)),  GetFloorAt(vector + new Vector3Int(1, 1, 0)) }
        };
    }

    // Has the potential to move in the wall?
    private void setMovingDirection(float[,] gradient, float DirectionInertia, Vector3 position, ref Vector3 direction)
    {
        int[] directions = new int[2] { -1, 1 };
        float gradientX = gradient[0, 0] + gradient[0, 1] - gradient[1, 0] - gradient[1, 1],
            gradientY = gradient[0, 0] + gradient[1, 0] - gradient[0, 1] - gradient[1, 1];
        direction.Set(
            // should it be dx + gx?
            (direction.x - gradientX) * DirectionInertia + gradientX,
            (direction.y - gradientY) * DirectionInertia + gradientY,
            0 // 2D gradient good enough for grounded z movement
        );

        float magnitude = direction.magnitude;
        if (magnitude <= Mathf.Epsilon)
        {
            // Not moving - pick random direction
            direction.x = directions[Random.Range(0, 2)];
            direction.y = directions[Random.Range(0, 2)];
            direction.z = 0;
        }
        else
        {
            direction /= magnitude;
        }
    }

    /// <summary>
    /// http://ranmantaru.com/blog/2011/10/08/water-erosion-on-heightmap-terrain/
    /// </summary>
    public void DropletErosion()
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
                Random.Range(0, Width - 1),
                Random.Range(0, Length - 1),
                Height - 1
            );
            position.z = GetFloorBelow(position);

            // Reset tracking parameters
            sediment = 0;
            speed = 0;
            water = 1;

            // Neighbour position.z values
            gradient = getGradient(position);

            // Moving droplet
            for (uint numMoves = 0; numMoves < maxMoves; ++numMoves)
            {
                // Surrounding ground absorbs water
                absorbAt(position, ref water);

                // Droplet moves downhill with inertia
                setMovingDirection(gradient, DirectionInertia, position, ref direction);

                // Next position
                nextPosition = position + direction;
                nextPosition.z = Mathf.Max(0, nextPosition.z);
                if (Mathf.FloorToInt(nextPosition.x) < 0 || Mathf.FloorToInt(nextPosition.x) + 1 >= Width || Mathf.FloorToInt(nextPosition.y) < 0 || Mathf.FloorToInt(nextPosition.y) + 1 >= Length) break; // Stop droplet if off map

                // Gradient modifiers
                nextPositionRemainder.x = nextPosition.x % 1;
                nextPositionRemainder.y = nextPosition.y % 1;
                if (nextPositionRemainder.x < 0) nextPositionRemainder.x = 0;
                if (nextPositionRemainder.y < 0) nextPositionRemainder.y = 0;

                // Deposited height of new point
                gradient = getGradient(new Vector3(nextPosition.x, nextPosition.y, GetFloorAt(nextPosition)));
                nextPosition.z = (
                        gradient[0, 0] * (1 - nextPositionRemainder.x)
                        + gradient[1, 0] * nextPositionRemainder.x
                    ) * (1 - nextPositionRemainder.y)
                    + (
                        gradient[0, 1] * (1 - nextPositionRemainder.x)
                        + gradient[1, 1] * nextPositionRemainder.x
                    ) * nextPositionRemainder.y;
                nextPosition.z = Mathf.Max(0, nextPosition.z);

                // If higher than current, try to deposit sediment up to neighbour height
                if (nextPosition.z >= position.z)
                {
                    sedimentCount = (nextPosition.z - position.z) + SedimentDeposit;
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
                sedimentCount = sediment - Mathf.Max(transport.z, MinSlope) * speed * water * SedimentCapacity;

                // Deposit/erode
                if (sedimentCount >= 0)
                {
                    sedimentCount *= DepositionSpeed;
                    deposit(sedimentCount, positionRemainder, ref transport);
                    sediment -= sedimentCount;
                }
                else
                {
                    // Don't erode more than transport.z
                    sedimentCount *= -ErosionSpeed;
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
                water *= 1 - EvaporationSpeed;

                // Move to neighbour
                position = nextPosition;
                positionRemainder = nextPositionRemainder;
            }
        }
    }

    private void depositAt(Vector3 vector, float w, float deltaSediment)
    {
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        int floor = (int)GetFloorAt(vector);
        float transport, fillPercent;

        for (float delta = w * deltaSediment; delta > 0; floor = (int)GetFloorAt(vector))
        {
            if (floor == Height)
            {
                Debug.LogWarning("Failed to deposit at " + vector + ": reached the top of the map " + floor + "/" + Height);
                break;
            }
            fillPercent = WorldMap[vectorInt.x, vectorInt.y, floor];
            transport = Mathf.Min(delta, 1 - fillPercent);
            delta -= transport;
            erosionMap[vectorInt.x, vectorInt.y, floor] += transport;
            WorldMap[vectorInt.x, vectorInt.y, floor] += transport;
            if (transport > 0) vector.z += transport;
            else ++vector.z;
        }

    }

    private void deposit(float deltaSediment, Vector3 vectorRemainder, ref Vector3 vector)
    {
        if (deltaSediment < 0)
        {
            Debug.LogError("Depositing negative value " + deltaSediment + " at " + vector);
        }
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        depositAt(vector, (1 - vectorRemainder.x) * (1 - vectorRemainder.y), deltaSediment);
        if (vectorInt.x + 1 < Width) depositAt(vector + new Vector3Int(1, 0, 0), vectorRemainder.x * (1 - vectorRemainder.y), deltaSediment);
        if (vectorInt.y + 1 < Length) depositAt(vector + new Vector3Int(0, 1, 0), (1 - vectorRemainder.x) * vectorRemainder.y, deltaSediment);
        if (vectorInt.x + 1 < Width && vectorInt.y + 1 < Length) depositAt(vector + new Vector3Int(1, 1, 0), vectorRemainder.x * vectorRemainder.y, deltaSediment);
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
        WorldMap[vectorInt.x, vectorInt.y, vectorInt.z] = Mathf.Max(0, WorldMap[vectorInt.x, vectorInt.y, vectorInt.z] - delta);
        float r = vector.x;
        float d = erosionMap[vectorInt.x, vectorInt.y, vectorInt.z];
        if (delta <= d) d -= delta;
        else
        {
            r += delta - d;
            d = 0;
            vector.x = Mathf.RoundToInt(r);
            if (vector.x < 0 || vector.x >= Width) return;
        }
        erosionMap[vectorInt.x, vectorInt.y, vectorInt.z] = d;
    }

    /// Note: http://libnoise.sourceforge.net/examples/worms/index.html
    public void GenerateCave(int x, int y, int z)
    {
        float speed = CaveRadius - 1;
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

        Vector3 headNoisePos = new Vector3(x, y, z);
        Vector3 headScreenPos = new Vector3(x, y, z);
        Vector3Int headScreenPosInt = new Vector3Int();

        float floor;
        for (int curSegment = 0; curSegment < CaveSegmentLength; ++curSegment)
        {
            // The angle of the head segment is used to determine the direction the worm
            // moves.  The worm moves in the opposite direction.
            noiseValue = caveNoise.DomainWarp(headNoisePos.x + curSegment * CaveTwistiness, headNoisePos.y, headNoisePos.z, Octaves, Lacunarity, Gain, Amplitude, 1 / InverseFrequency);
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
            if (headScreenPos.z > Height || headScreenPos.z < 0) continue;
            headScreenPos.x = Mathf.Clamp(headScreenPos.x, 0, Width - 1);
            headScreenPos.y = Mathf.Clamp(headScreenPos.y, 0, Length - 1);

            headScreenPosInt.Set(
                (int)headScreenPos.x,
                (int)headScreenPos.y,
                (int)headScreenPos.z
                );

            variedRadius = CaveRadius;

            if (CaveRadiusVarianceScale != 0)
            {
                noiseValue = caveNoise.DomainWarp((headNoisePos.x + radiusVarianceOffset.x) + curSegment * CaveTwistiness, headNoisePos.y + radiusVarianceOffset.y, headNoisePos.z + radiusVarianceOffset.z, Octaves, Lacunarity, Gain, Amplitude, 1 / InverseFrequency);
                if (variedRadius == 0)
                {
                    variedRadius = (0.5f - noiseValue) * CaveRadiusVarianceScale;
                }
                else
                {
                    variedRadius += CaveRadius * (0.5f - noiseValue) * CaveRadiusVarianceScale;
                }
            }

            // Remove land at head of worm
            if (variedRadius <= 0)
            {
                WorldMap[headScreenPosInt.x, headScreenPosInt.y, headScreenPosInt.z] = 0;
            }
            else
            {
                if (variedRadius < 1)
                {
                    WorldMap[headScreenPosInt.x, headScreenPosInt.y, headScreenPosInt.z] = 0;
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
                                if (headScreenPosInt.z + k >= Height || headScreenPosInt.z + k <= 0) continue; // 0 is always ground
                                if (i * i + j * j + k * k <= variedRadius * variedRadius) // Inside sphere
                                {
                                    WorldMap[headScreenPosInt.x + i, headScreenPosInt.y + j, headScreenPosInt.z + k] = 0;
                                    floor = GetFloorAt(headScreenPosInt + new Vector3(i, j, k));
                                    if (floor < MinHeight)
                                    {
                                        MinHeight = floor;
                                    }
                                }
                            }
                        }
                    }
                }

                // Move to end of CaveRadius
                speed = (CaveRadius - variedRadiusInt) - 1;
                if (speed < 1)
                {
                    speed = 1;
                }
            }
        }
    }

    /// <summary>
    /// Resets noise
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        if (heightMapNoise != null)
        {
            heightMapNoise.ResetGradientArray();
        }
        if (caveNoise != null)
        {
            caveNoise.ResetGradientArray();
        }
    }
}
