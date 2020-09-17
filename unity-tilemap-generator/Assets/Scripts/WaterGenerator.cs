using UnityEngine;

class WaterGenerator : Generator
{
    public float WaterLevel;

    private TerrainGenerator terrainGenerator;

    public WaterGenerator(TerrainGenerator terrainGenerator)
    {
        this.terrainGenerator = terrainGenerator;
        WorldMap = new float[terrainGenerator.Width, terrainGenerator.Length, terrainGenerator.Height];
        Width = terrainGenerator.Width;
        Length = terrainGenerator.Length;
        Height = terrainGenerator.Height;
    }

    /// <summary>
    /// Fills map with water
    /// </summary>
    public void Fill()
    {
        applyConsistency();

        float fillLevel;
        if (WaterLevel >= Height)
        {
            Debug.LogWarning("WaterLevel higher than map height - filling at maximum height");
            fillLevel = Height - 1;
        }
        else
        {
            fillLevel = WaterLevel - 1;
        }

        Vector3 vector = new Vector3();
        Vector3Int vectorInt = new Vector3Int();
        int floor;
        for (vector.x = 0; vector.x < Width; ++vector.x)
        {
            vectorInt.x = (int)vector.x;
            for (vector.y = 0; vector.y < Length; ++vector.y)
            {
                vectorInt.y = (int)vector.y;
                vector.z = vectorInt.z = Height - 1;
                floor = (int)terrainGenerator.GetFloorBelow(vector);
                for (vector.z = fillLevel; vector.z >= floor && floor >= 0; --vector.z)
                {
                    vectorInt.z = (int)vector.z;
                    WorldMap[vectorInt.x, vectorInt.y, vectorInt.z] = 1 - terrainGenerator.WorldMap[vectorInt.x, vectorInt.y, vectorInt.z];
                }
            }
        }
    }

    /// <summary>
    /// Creates rivers where excess wetness exists in terrain
    /// </summary>
    /// <note>
    /// Fill percent can be more than one
    /// </note>
    public void FillExcessWetness()
    {
        applyConsistency();

        Vector3 vector = new Vector3();
        Vector3Int vectorInt = new Vector3Int();
        float waterHeight;
        for (vector.x = 0; vector.x < terrainGenerator.Width; ++vector.x)
        {
            vectorInt.x = (int)vector.x;
            for (vector.y = 0; vector.y < terrainGenerator.Length; ++vector.y)
            {
                vectorInt.y = (int)vector.y;
                vector.z = vectorInt.z = Height - 1;
                for (vector.z = terrainGenerator.GetFloorAt(vector); vector.z >= 0; --vector.z)
                {
                    vectorInt.z = (int)vector.z;
                    if (vectorInt.z < Height
                        && (int)terrainGenerator.GetFloorAt(vector) == vectorInt.z // Vector is on the ground
                        && terrainGenerator.WetnessMap[vectorInt.x, vectorInt.y, vectorInt.z] >= terrainGenerator.AbsorptionCapacity
                        && WorldMap[vectorInt.x, vectorInt.y, vectorInt.z] < terrainGenerator.WetnessMap[vectorInt.x, vectorInt.y, vectorInt.z])
                    {
                        WorldMap[vectorInt.x, vectorInt.y, vectorInt.z] = terrainGenerator.WetnessMap[vectorInt.x, vectorInt.y, vectorInt.z];
                        waterHeight = vector.z + WorldMap[vectorInt.x, vectorInt.y, vectorInt.z];
                        if (MinHeight > waterHeight) MinHeight = waterHeight;
                        if (MaxHeight < waterHeight) MaxHeight = waterHeight;
                    }
                }
            }
        }
    }

    private void applyConsistency()
    {
        if (Width != terrainGenerator.Width
            || Length != terrainGenerator.Length
            || Height != terrainGenerator.Height)
        {
            Debug.LogError("WaterGenerator inconsistent with last reset: Force resetting WaterGenerator");
            Reset();
        }
    }

    public override void Reset()
    {
        base.Reset();
        WorldMap = new float[terrainGenerator.Width, terrainGenerator.Length, terrainGenerator.Height];
        Width = terrainGenerator.Width;
        Length = terrainGenerator.Length;
        Height = terrainGenerator.Height;
    }
}