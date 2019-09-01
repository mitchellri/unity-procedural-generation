using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Added
using UnityEngine.Tilemaps;

public class TilemapGenerator : MonoBehaviour
{
    // Parameters
    public Tilemap Floor;
    public TileBase FloorTile;
    public TileBase WaterTile;
    public TileBase SnowTile;
    public int Width;
    public int Height;
    public int MinimumSmoothness;
    public int MaximumSmoothness;
    public int WaterLevel;
    public int SnowLevel;
    // Private members
    private PerlinNoise perlinNoise;
    private static readonly float colorIncrement = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        perlinNoise = new PerlinNoise(Width, Height);
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        // Not generating anything new
        if (Input.GetKey(KeyCode.Space))
        {
            perlinNoise.ResetGradientArray();
            Floor.ClearAllTiles();
            Generate();
        }
    }

    public void GenerateTerrain()
    {
        Vector3Int min = Floor.cellBounds.min;
        int smoothness = Random.Range(MinimumSmoothness, MaximumSmoothness);
        int z;
        Vector3Int tile = new Vector3Int();
        Color color;
        for (int x = 0; x < Width; ++x)
        {
            tile.x = min.x + x;
            for (int y = 0; y < Height; ++y)
            {
                tile.y = min.y + y;
                z = (int)Mathf.Round(perlinNoise.Perlin((float)x / smoothness, (float)y / smoothness) * 10);
                tile.z = min.z + z;
                if (!Floor.HasTile(tile))
                {
                    if (z < WaterLevel) Floor.SetTile(tile, WaterTile);
                    else if (z > SnowLevel) Floor.SetTile(tile, SnowTile);
                    else Floor.SetTile(tile, FloorTile);
                    Floor.SetTileFlags(tile, TileFlags.None);
                }
                color = Floor.GetColor(tile);
                if (z > SnowLevel)
                {
                    color.r -= colorIncrement * (z - SnowLevel);
                    color.g -= colorIncrement * (z - SnowLevel);
                }
                else if (z < WaterLevel) color.g += colorIncrement * z;
                else color.g += colorIncrement * z;
                Floor.SetColor(tile, color);
            }
        }
        return;
    }
    public void Generate()
    {
        GenerateTerrain();
        return;
    }
}
