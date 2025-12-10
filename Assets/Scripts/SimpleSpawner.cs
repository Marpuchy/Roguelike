using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SimpleSpawner: spawnea Food, Walls y Enemies según configuraciones serializables (foldouts en Inspector).
/// BoardManager puede asignar/configurar estos campos antes de llamar SpawnAll(...).
/// </summary>
public class SimpleSpawner : MonoBehaviour
{
    public enum SpawnMode { FixedCorners, Random, Procedural }

    [System.Serializable]
    public class FoodConfig
    {
        public SpawnMode mode = SpawnMode.Random;
        public FoodObject[] prefabs;
        public int minCount = 1;
        public int maxCount = 3;

        // procedural
        public int cellsPer = 40;
        public float levelMultiplier = 0.5f;
        public int baseCount = 0;
        public bool roundUp = false;
        public int maxCap = 0;
    }

    [System.Serializable]
    public class WallConfig
    {
        public SpawnMode mode = SpawnMode.Random;
        public WallObject[] prefabs;
        public int baseMin = 6;
        public int baseMax = 10;

        // procedural
        public int cellsPer = 50;
        public float levelMultiplier = 0.2f;
        public int baseCount = 0;
        public bool roundUp = false;
        public int maxCap = 0;
    }

    [System.Serializable]
    public class EnemyConfig
    {
        public SpawnMode mode = SpawnMode.Procedural;
        public Enemy prefab;
        public int randomMin = 1;
        public int randomMax = 3;

        // procedural
        public int cellsPer = 40;
        public float levelMultiplier = 0.5f;
        public int baseCount = 0;
        public bool roundUp = false;
        public int maxCap = 0;
    }

    [Header("Configs (edítalas en BoardManager o aquí)")]
    public FoodConfig food = new FoodConfig();
    public WallConfig walls = new WallConfig();
    public EnemyConfig enemies = new EnemyConfig();

    // exposiciones útiles para que otros (BoardManager) puedan leer/usar prefabs del spawner
    public WallObject[] WallPrefabs => walls.prefabs;

    /// <summary>
    /// SpawnAll: consume la lista emptyCells (quita celdas ocupadas) y usa BoardManager.AddObject para colocar instancias.
    /// </summary>
    public void SpawnAll(List<Vector2Int> emptyCells, BoardManager board)
    {
        if (emptyCells == null || emptyCells.Count == 0 || board == null) return;

        SpawnWalls(emptyCells, board);
        SpawnFood(emptyCells, board);
        SpawnEnemies(emptyCells, board);
    }

    // --- Walls ---
    void SpawnWalls(List<Vector2Int> emptyCells, BoardManager board)
    {
        if (walls.prefabs == null || walls.prefabs.Length == 0) return;

        switch (walls.mode)
        {
            case SpawnMode.FixedCorners:
                TryPlaceAt(emptyCells, board, new Vector2Int(0, 0), walls.prefabs);
                TryPlaceAt(emptyCells, board, new Vector2Int(board.Width - 1, 0), walls.prefabs);
                TryPlaceAt(emptyCells, board, new Vector2Int(0, board.Height - 1), walls.prefabs);
                TryPlaceAt(emptyCells, board, new Vector2Int(board.Width - 1, board.Height - 1), walls.prefabs);
                break;

            case SpawnMode.Random:
                int count = Random.Range(walls.baseMin, walls.baseMax + 1);
                SpawnRandomObjects(emptyCells, board, walls.prefabs, count);
                break;

            case SpawnMode.Procedural:
                int area = board.Width * board.Height;
                float fromArea = (float)area / Mathf.Max(1, walls.cellsPer);
                float scaled = fromArea * (walls.levelMultiplier * GetCurrentLevel());
                float suggested = walls.baseCount + scaled;
                int wCount = walls.roundUp ? Mathf.CeilToInt(suggested) : Mathf.FloorToInt(suggested);
                if (walls.maxCap > 0) wCount = Mathf.Min(wCount, walls.maxCap);
                wCount = Mathf.Clamp(wCount, 0, emptyCells.Count);
                SpawnRandomObjects(emptyCells, board, walls.prefabs, wCount);
                break;
        }
    }

    // --- Food ---
    void SpawnFood(List<Vector2Int> emptyCells, BoardManager board)
    {
        if (food.prefabs == null || food.prefabs.Length == 0) return;

        switch (food.mode)
        {
            case SpawnMode.FixedCorners:
                TryPlaceAt(emptyCells, board, new Vector2Int(0, 0), food.prefabs);
                TryPlaceAt(emptyCells, board, new Vector2Int(board.Width - 1, 0), food.prefabs);
                TryPlaceAt(emptyCells, board, new Vector2Int(0, board.Height - 1), food.prefabs);
                TryPlaceAt(emptyCells, board, new Vector2Int(board.Width - 1, board.Height - 1), food.prefabs);
                break;

            case SpawnMode.Random:
                int fBase = Random.Range(food.minCount, food.maxCount + 1);
                SpawnRandomObjects(emptyCells, board, food.prefabs, Mathf.Min(fBase, emptyCells.Count));
                break;

            case SpawnMode.Procedural:
                int area = board.Width * board.Height;
                float fromArea = (float)area / Mathf.Max(1, food.cellsPer);
                float scaled = fromArea * (food.levelMultiplier * GetCurrentLevel());
                float suggested = food.baseCount + scaled;
                int fCount = food.roundUp ? Mathf.CeilToInt(suggested) : Mathf.FloorToInt(suggested);
                if (food.maxCap > 0) fCount = Mathf.Min(fCount, food.maxCap);
                fCount = Mathf.Clamp(fCount, 0, emptyCells.Count);
                SpawnRandomObjects(emptyCells, board, food.prefabs, fCount);
                break;
        }
    }

    // --- Enemies ---
    void SpawnEnemies(List<Vector2Int> emptyCells, BoardManager board)
    {
        if (enemies.prefab == null) return;

        switch (enemies.mode)
        {
            case SpawnMode.FixedCorners:
                TryPlaceEnemyAt(emptyCells, board, new Vector2Int(0, 0));
                TryPlaceEnemyAt(emptyCells, board, new Vector2Int(board.Width - 1, 0));
                TryPlaceEnemyAt(emptyCells, board, new Vector2Int(0, board.Height - 1));
                TryPlaceEnemyAt(emptyCells, board, new Vector2Int(board.Width - 1, board.Height - 1));
                break;

            case SpawnMode.Random:
                int rCount = Random.Range(enemies.randomMin, enemies.randomMax + 1);
                SpawnRandomEnemies(emptyCells, board, rCount);
                break;

            case SpawnMode.Procedural:
                int area = board.Width * board.Height;
                float fromArea = (float)area / Mathf.Max(1, enemies.cellsPer);
                float scaled = fromArea * (enemies.levelMultiplier * GetCurrentLevel());
                float suggested = enemies.baseCount + scaled;
                int eCount = enemies.roundUp ? Mathf.CeilToInt(suggested) : Mathf.FloorToInt(suggested);
                if (enemies.maxCap > 0) eCount = Mathf.Min(eCount, enemies.maxCap);
                eCount = Mathf.Clamp(eCount, 0, emptyCells.Count);
                SpawnRandomEnemies(emptyCells, board, eCount);
                break;
        }
    }

    // ----------------- Helpers -----------------
    void TryPlaceAt<T>(List<Vector2Int> emptyCells, BoardManager board, Vector2Int pos, T[] prefabs) where T : CellObject
    {
        if (prefabs == null || prefabs.Length == 0) return;
        if (pos.x < 0 || pos.x >= board.Width || pos.y < 0 || pos.y >= board.Height) return;
        if (!emptyCells.Contains(pos)) return;
        emptyCells.Remove(pos);
        T prefab = prefabs[Random.Range(0, prefabs.Length)];
        T inst = Instantiate(prefab);
        board.AddObject(inst, pos);
    }

    void TryPlaceEnemyAt(List<Vector2Int> emptyCells, BoardManager board, Vector2Int pos)
    {
        if (enemies.prefab == null) return;
        if (pos.x < 0 || pos.x >= board.Width || pos.y < 0 || pos.y >= board.Height) return;
        if (!emptyCells.Contains(pos)) return;
        emptyCells.Remove(pos);
        Enemy e = Instantiate(enemies.prefab);
        board.AddObject(e, pos);
    }

    void SpawnRandomObjects<T>(List<Vector2Int> emptyCells, BoardManager board, T[] prefabs, int count) where T : CellObject
    {
        if (prefabs == null || prefabs.Length == 0 || emptyCells == null || emptyCells.Count == 0) return;
        count = Mathf.Min(count, emptyCells.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, emptyCells.Count);
            Vector2Int coord = emptyCells[idx];
            emptyCells.RemoveAt(idx);
            T prefab = prefabs[Random.Range(0, prefabs.Length)];
            T inst = Instantiate(prefab);
            board.AddObject(inst, coord);
        }
    }

    void SpawnRandomEnemies(List<Vector2Int> emptyCells, BoardManager board, int count)
    {
        if (enemies.prefab == null || emptyCells == null || emptyCells.Count == 0) return;
        count = Mathf.Min(count, emptyCells.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, emptyCells.Count);
            Vector2Int coord = emptyCells[idx];
            emptyCells.RemoveAt(idx);
            Enemy e = Instantiate(enemies.prefab);
            board.AddObject(e, coord);
        }
    }

    int GetCurrentLevel()
    {
        var gm = GameManager.Instance;
        if (gm == null) return 1;

        // 1) intenta usar la propiedad pública CurrentLevel (más robusta)
        var prop = gm.GetType().GetProperty("CurrentLevel");
        if (prop != null)
        {
            int val = (int)prop.GetValue(gm);
            Debug.Log($"[SimpleSpawner] GetCurrentLevel() via property -> {val}");
            return val;
        }

        // 2) fallback: intenta leer campo privado m_CurrentLevel (retrocompatibilidad)
        var field = gm.GetType().GetField("m_CurrentLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            int val = (int)field.GetValue(gm);
            Debug.Log($"[SimpleSpawner] GetCurrentLevel() via field -> {val}");
            return val;
        }

        // default
        Debug.Log("[SimpleSpawner] GetCurrentLevel() fallback -> 1");
        return 1;
    }

}
