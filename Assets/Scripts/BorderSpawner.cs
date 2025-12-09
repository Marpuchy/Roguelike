using UnityEngine;
using UnityEngine.Tilemaps;

public static class BorderSpawner
{
    public static void CreateOuterBorder(Tilemap tilemap, Tile wallTile, int width, int height, int thickness = 1, GameObject wallPrefab = null, Transform parent = null)
    {
        if (tilemap == null || wallTile == null || thickness <= 0) return;

        int minX = -thickness;
        int maxX = (width - 1) + thickness;
        int minY = -thickness;
        int maxY = (height - 1) + thickness;

        for (int x = minX; x <= maxX; x++)
        {
            for (int t = 0; t < thickness; t++)
            {
                tilemap.SetTile(new Vector3Int(x, -1 - t, 0), wallTile);
                tilemap.SetTile(new Vector3Int(x, height + t, 0), wallTile);
            }
        }

        for (int y = minY; y <= maxY; y++)
        {
            for (int t = 0; t < thickness; t++)
            {
                tilemap.SetTile(new Vector3Int(-1 - t, y, 0), wallTile);
                tilemap.SetTile(new Vector3Int(width + t, y, 0), wallTile);
            }
        }

        if (wallPrefab != null)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    Spawn(tilemap, wallPrefab, new Vector3Int(x, -1 - t, 0), parent);
                    Spawn(tilemap, wallPrefab, new Vector3Int(x, height + t, 0), parent);
                }
            }
            for (int y = minY; y <= maxY; y++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    Spawn(tilemap, wallPrefab, new Vector3Int(-1 - t, y, 0), parent);
                    Spawn(tilemap, wallPrefab, new Vector3Int(width + t, y, 0), parent);
                }
            }
        }
    }

    static void Spawn(Tilemap tilemap, GameObject prefab, Vector3Int cell, Transform parent)
    {
        Vector3 world = tilemap.CellToWorld(cell) + tilemap.cellSize / 2f;
        GameObject go = Object.Instantiate(prefab, world, Quaternion.identity);
        if (parent != null) go.transform.SetParent(parent, true);
    }
}
