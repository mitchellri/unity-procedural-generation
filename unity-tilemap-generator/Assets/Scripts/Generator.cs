using UnityEngine;

abstract class Generator
{
    /// <summary>
    /// 0 is the bottom of the map
    /// </summary>
    public float[,,] WorldMap { get; set; }
    public virtual int Width { get; set; }
    public virtual int Length { get; set; }
    /// <summary>
    /// Total generated height of map
    /// </summary>
    public virtual int Height { get; set; }
    /// <summary>
    /// Minimum floor tile in map
    /// </summary>
    public float MinHeight { get; protected set; } = float.MaxValue;
    /// <summary>
    /// Maximum floor tile in map
    /// </summary>
    public float MaxHeight { get; protected set; } = float.MinValue;

    /// <summary>
    /// Gets next ground block going downward starting at vector
    /// </summary>
    /// <param name="vector">Initial position [Inclusive]</param>
    /// <returns>Floor height if exists, or -1 if no floor at vector or below</returns>
    public float GetFloorBelow(Vector3 vector)
    {
        if (vector.z == 0) return 0; // Bottom of the map, nothing under
        else if (vector.z == Height) vector.z = Height - 1; // Top of map out of range
            Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        for (int z = vectorInt.z; z >= 0; --z)
        {
            if (WorldMap[vectorInt.x, vectorInt.y, z] > 0)
            {
                return z + WorldMap[vectorInt.x, vectorInt.y, z];
            }
        }
        return 0; // Floor of the map
    }

    /// <summary>
    /// Gets next ground block with open air above going upward starting at vector
    /// </summary>
    /// <param name="vector">Initial position [Inclusive]</param>
    /// <returns>Floor height if exists, or -1 if no floor with open air above at vector or above</returns>
    public float GetFloorAbove(Vector3 vector)
    {
        if (vector.z == Height) return vector.z;  // Top of the map, out of bounds of array
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );

        for (int z = vectorInt.z; z < Height; ++z)
        {
            if (WorldMap[vectorInt.x, vectorInt.y, z] < 1)
            {
                return z + WorldMap[vectorInt.x, vectorInt.y, z];
            }
        }

        if (Height - 1 + WorldMap[vectorInt.x, vectorInt.y, Height - 1] == Height) return Height; // Went to top of the map
        else return -1;
    }

    /// <summary>
    /// Floor from the perspective of vector's z value
    /// </summary>
    /// <returns>Floor below if airborne, or floor at/above if ground exists</returns>
    public float GetFloorAt(Vector3 vector)
    {
        if (vector.z == Height) return vector.z;
        Vector3Int vectorInt = new Vector3Int(
            (int)vector.x,
            (int)vector.y,
            (int)vector.z
            );
        float floor = -1;
        if (WorldMap[vectorInt.x, vectorInt.y, vectorInt.z] > 0) floor = GetFloorAbove(vector);
        else floor = GetFloorBelow(vector);
        return floor;
    }

    public virtual void Reset()
    {
        WorldMap = null;
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;
    }
}