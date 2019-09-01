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
    // Private members
    private PerlinNoise perlinNoise;
    private const float waterLevel = 0;
    private static readonly int colorIncrement = 1;
    // Start is called before the first frame update
    void Start()
    {
        Vector3Int size = Floor.cellBounds.size;
        perlinNoise = new PerlinNoise(size.x, size.y);
        Generate();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GenerateTerrain()
    {
        Vector3Int size = Floor.cellBounds.size;
        Vector3Int min = Floor.cellBounds.min;
        int width = size.x;
        int height = size.y;
        int smoothness = Random.Range(2, 3);
        int z;
        Vector3Int tile = new Vector3Int();
        Color color;
        Color oldColor;
        for (int x = 0; x < width; ++x)
        {
            tile.x = min.x + x;
            for (int y = 0; y < height; ++y)
            {
                tile.y = min.y + y;
                z = (int)Mathf.Round(perlinNoise.Perlin((float)x / smoothness, (float)y / smoothness) * 10);
                tile.z = min.z + z;
                Floor.SetTile(tile, FloorTile);
                Floor.SetTileFlags(tile, TileFlags.None);
                color = Floor.GetColor(tile);
                    oldColor = Floor.GetColor(tile);
                color.g += colorIncrement*z;
                Debug.Log("Color Diff:" + (color-oldColor));
                Floor.SetColor(tile,color);
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
