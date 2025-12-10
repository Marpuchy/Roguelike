using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

/// <summary>
/// BoardManager (nuevo):
/// - Usa SimpleSpawner para spawns.
/// - Expone Width/Height públicos (necesario para otros componentes).
/// - Incluye logs de depuración y fallbacks para asegurar que los prefabs lleguen al SimpleSpawner.
/// - Crea automáticamente un SimpleSpawner en runtime si no hay ninguno y no se ha asignado SpawnerPrefab.
/// - Opcional: forzar procedural temporalmente para pruebas.
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("Tamaño")]
    public int baseSize = 8;
    public int maxSize = 200;

    // Exponer Width/Height públicamente (lectura externa)
    public int Width { get; private set; }
    public int Height { get; private set; }

    [FormerlySerializedAs("GroundTiles")] [Header("Tiles (visual)")]
    public Tile[] groundTiles;
    [FormerlySerializedAs("WallTiles")] public Tile[] wallTiles; // para el borde visual
    [FormerlySerializedAs("ExitCellPrefab")] public ExitCellObject exitCellPrefab;

    [Header("Spawner (nuevo)")]
    public SimpleSpawner.FoodConfig spawnFood = new SimpleSpawner.FoodConfig();
    public SimpleSpawner.WallConfig spawnWalls = new SimpleSpawner.WallConfig();
    public SimpleSpawner.EnemyConfig spawnEnemies = new SimpleSpawner.EnemyConfig();
    [FormerlySerializedAs("SpawnerPrefab")] public SimpleSpawner spawnerPrefab; // opcional: prefab con configuraciones ya asignadas

    [FormerlySerializedAs("Player")] [Header("Opcional / Player")]
    public PlayerController player;

    [Header("Debug / testing")]
    [Tooltip("Si true, fuerza el modo Procedural en food/walls/enemies antes de SpawnAll (temporal para pruebas).")]
    public bool forceProceduralForAll = false;
    [Tooltip("Si true, imprime un pequeño resumen después de SpawnAll.")]
    public bool logSpawnSummary = false;

    // internals
    Tilemap m_Tilemap;
    Grid m_Grid;
    CellData[,] m_BoardData;
    List<Vector2Int> m_EmptyCellsList;
    List<GameObject> m_BorderObjects;

    // runtime spawner
    SimpleSpawner m_Spawner;

    [Obsolete("Obsolete")]
    void Awake()
    {
        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Grid == null) m_Grid = GetComponentInChildren<Grid>();

        // Intentar encontrar SimpleSpawner ya configurado en la escena
        m_Spawner = FindObjectOfType<SimpleSpawner>();
        if (m_Spawner == null)
        {
            var goByName = GameObject.Find("SimpleSpawner");
            if (goByName != null) m_Spawner = goByName.GetComponent<SimpleSpawner>();
        }

        // Si no hay y tenemos un prefab, instanciamos
        if (m_Spawner == null && spawnerPrefab != null)
        {
            GameObject go = Instantiate(spawnerPrefab.gameObject);
            go.name = "SimpleSpawner";
            m_Spawner = go.GetComponent<SimpleSpawner>();
            Debug.Log("[BoardManager] SimpleSpawner instanciado desde SpawnerPrefab.");
        }

        // --- NUEVO: si aún no hay spawner, creamos uno *y copiamos las configuraciones desde BoardManager* ---
        if (m_Spawner == null)
        {
            GameObject go = new GameObject("SimpleSpawner");
            m_Spawner = go.AddComponent<SimpleSpawner>();
            Debug.Log("[BoardManager] Created runtime SimpleSpawner (auto).");

            // Copiar configuraciones inspector -> runtime spawner (fallback)
            try
            {
                // Copy whole configs (struct/class assignment) when possible
                // This will copy prefabs arrays and other fields inside the config structs/classes
                m_Spawner.food = spawnFood;
                m_Spawner.walls = spawnWalls;
                m_Spawner.enemies = spawnEnemies;

                Debug.Log("[BoardManager] Copied spawnFood/spawnWalls/spawnEnemies to runtime SimpleSpawner.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[BoardManager] Exception copying spawner configs: " + ex.Message);
            }
        }

        // Ahora m_Spawner siempre será null *solo* si algo ha ido muy mal; avisamos en consola
        if (m_Spawner == null)
        {
            Debug.LogWarning("[BoardManager] No se encontró SimpleSpawner en escena ni SpawnerPrefab asignado, y no se pudo crear uno en runtime. No se spawneará nada.");
        }
    }

    // ----------------- API pública -----------------
    public void Init() => InitLevel(1);

    /// <summary>
    /// InitLevel: baseSize + (level-1) => tamaño total visible. Interior = totalSize
    /// </summary>
    public void InitLevel(int level)
    {
        Debug.Log($"[BoardManager] InitLevel called with level={level} (baseSize={baseSize}, maxSize={maxSize})");
        int totalSize = baseSize + Mathf.Max(0, level - 1);
        totalSize = Mathf.Clamp(totalSize, 3, maxSize);
        Init(totalSize, totalSize);
    }

    [Obsolete("Obsolete")]
    public void Init(int width, int height)
    {
        width = Mathf.Clamp(width, 3, maxSize);
        height = Mathf.Clamp(height, 3, maxSize);

        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Grid == null) m_Grid = GetComponentInChildren<Grid>();

        // limpiamos anteriores
        ClearInternal();

        // asignar propiedades públicas para que otros scripts las lean
        Width = width;
        Height = height;
        Debug.Log($"[BoardManager] Init width={Width}, height={Height}");

        m_BoardData = new CellData[Width, Height];
        m_EmptyCellsList = new List<Vector2Int>();
        m_BorderObjects = new List<GameObject>();

        // rellenar tiles y marcar celdas vacías
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Tile tile = null;
                m_BoardData[x, y] = new CellData();

                bool isBorder = (x == 0 || y == 0 || x == Width - 1 || y == Height - 1);
                if (isBorder)
                {
                    if (wallTiles != null && wallTiles.Length > 0)
                        tile = wallTiles[Random.Range(0, wallTiles.Length)];
                    m_BoardData[x, y].Passable = false;
                }
                else
                {
                    if (groundTiles != null && groundTiles.Length > 0)
                        tile = groundTiles[Random.Range(0, groundTiles.Length)];
                    m_BoardData[x, y].Passable = true;
                    m_EmptyCellsList.Add(new Vector2Int(x, y));
                }

                if (m_Tilemap != null) m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        // reservar y colocar player en (1,1) si existe
        Vector2Int playerPos = new Vector2Int(1, 1);
        if (m_EmptyCellsList.Contains(playerPos)) m_EmptyCellsList.Remove(playerPos);
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.Spawn(this, playerPos);
        }

        // colocar exit en Width-2,Height-2 (si hay prefab)
        Vector2Int exitPos = new Vector2Int(Width - 2, Height - 2);
        if (m_EmptyCellsList.Contains(exitPos)) m_EmptyCellsList.Remove(exitPos);
        if (exitCellPrefab != null)
        {
            AddObject(Instantiate(exitCellPrefab), exitPos);
        }

        // Pasar configuraciones al SimpleSpawner (si existe) y delegar spawn
        if (m_Spawner != null)
        {
            // copia por valor las configs inspectorales
            m_Spawner.food = spawnFood;
            m_Spawner.walls = spawnWalls;
            m_Spawner.enemies = spawnEnemies;

            // Fallbacks: asegurar que las referencias a prefabs estén presentes en el spawner runtime
            if ((m_Spawner.food.prefabs == null || m_Spawner.food.prefabs.Length == 0) && (spawnFood.prefabs != null && spawnFood.prefabs.Length > 0))
            {
                m_Spawner.food.prefabs = spawnFood.prefabs;
                Debug.Log("[BoardManager] Fallback: copied spawnFood.prefabs to m_Spawner.food.prefabs");
            }
            if ((m_Spawner.walls.prefabs == null || m_Spawner.walls.prefabs.Length == 0) && (spawnWalls.prefabs != null && spawnWalls.prefabs.Length > 0))
            {
                m_Spawner.walls.prefabs = spawnWalls.prefabs;
                Debug.Log("[BoardManager] Fallback: copied spawnWalls.prefabs to m_Spawner.walls.prefabs");
            }
            if (m_Spawner.enemies.prefab == null && spawnEnemies.prefab != null)
            {
                m_Spawner.enemies.prefab = spawnEnemies.prefab;
                Debug.Log("[BoardManager] Fallback: copied spawnEnemies.prefab to m_Spawner.enemies.prefab");
            }

            // Opcional: forzar procedural para pruebas rápidas
            if (forceProceduralForAll)
            {
                m_Spawner.food.mode = SimpleSpawner.SpawnMode.Procedural;
                m_Spawner.walls.mode = SimpleSpawner.SpawnMode.Procedural;
                m_Spawner.enemies.mode = SimpleSpawner.SpawnMode.Procedural;
                Debug.Log("[BoardManager] forceProceduralForAll=true -> forced Procedural mode for all spawns (temporary).");
            }

            // DEBUG: imprimir estado antes de SpawnAll
            int emptyCount = (m_EmptyCellsList == null) ? 0 : m_EmptyCellsList.Count;
            int foodPrefabs = (m_Spawner.food.prefabs == null) ? 0 : m_Spawner.food.prefabs.Length;
            int wallPrefabs = (m_Spawner.walls.prefabs == null) ? 0 : m_Spawner.walls.prefabs.Length;
            int enemyPref = (m_Spawner.enemies.prefab == null) ? 0 : 1;

            Debug.Log($"[BoardManager] About to SpawnAll: emptyCells={emptyCount}, foodPrefabs={foodPrefabs}, wallPrefabs={wallPrefabs}, enemyPrefab={(enemyPref==1?"yes":"no")}, food.mode={m_Spawner.food.mode}, walls.mode={m_Spawner.walls.mode}, enemies.mode={m_Spawner.enemies.mode}");

            m_Spawner.SpawnAll(m_EmptyCellsList, this);

            if (logSpawnSummary)
            {
                Debug.Log("[BoardManager] SpawnAll executed. (enable verbose logs in SimpleSpawner for counts).");
            }
        }
        else
        {
            Debug.LogWarning("[BoardManager] No SimpleSpawner configurado: no se spawnearán objetos (añade un SimpleSpawner en escena o asigna SpawnerPrefab).");
        }
    }

    // ----------------- Helpers / API -----------------
    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        if (m_Grid == null) m_Grid = GetComponentInChildren<Grid>();
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    public CellData CellData(Vector2Int cellIndex)
    {
        if (m_BoardData == null) return null;
        if (cellIndex.x < 0 || cellIndex.x >= Width || cellIndex.y < 0 || cellIndex.y >= Height) return null;
        return m_BoardData[cellIndex.x, cellIndex.y];
    }

    public void AddObject(CellObject obj, Vector2Int coord)
    {
        if (m_BoardData == null) return;
        if (coord.x < 0 || coord.x >= Width || coord.y < 0 || coord.y >= Height) return;
        CellData data = m_BoardData[coord.x, coord.y];
        if (data == null) return;
        obj.transform.position = CellToWorld(coord);
        data.ContainedObject = obj;
        obj.Init(coord);
    }

    public void SetCellTile(Vector2Int cellIndex, Tile tile)
    {
        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Tilemap != null) m_Tilemap.SetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0), tile);
    }

    public Tile GetCelltile(Vector2Int cellIndex)
    {
        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Tilemap == null) return null;
        return m_Tilemap.GetTile<Tile>(new Vector3Int(cellIndex.x, cellIndex.y, 0));
    }

    // ----------------- Clear / cleanup -----------------
    public void Clear()
    {
        if (m_BoardData == null) return;

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                var cell = m_BoardData[x, y];
                if (cell != null && cell.ContainedObject != null)
                {
                    Destroy(cell.ContainedObject.gameObject);
                }
                if (m_Tilemap != null) SetCellTile(new Vector2Int(x, y), null);
            }
        }

        m_BoardData = null;
        if (m_EmptyCellsList != null) m_EmptyCellsList.Clear();
        if (m_BorderObjects != null) m_BorderObjects.Clear();

        // Reset Width/Height a 0 para detectar lecturas prematuras
        Width = 0;
        Height = 0;
    }

    void ClearInternal()
    {
        // destruir objetos lógicos previos
        if (m_BoardData != null)
        {
            for (int y = 0; y < m_BoardData.GetLength(1); y++)
            {
                for (int x = 0; x < m_BoardData.GetLength(0); x++)
                {
                    if (m_BoardData[x, y]?.ContainedObject != null)
                        Destroy(m_BoardData[x, y].ContainedObject.gameObject);
                }
            }
        }

        // limpiar tiles interiores previos si existían
        if (m_Tilemap != null)
        {
            int w = (m_BoardData != null) ? m_BoardData.GetLength(0) : 0;
            int h = (m_BoardData != null) ? m_BoardData.GetLength(1) : 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    m_Tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }

        // destruir border objects instanciados previamente
        if (m_BorderObjects != null)
        {
            foreach (var bo in m_BorderObjects) if (bo != null) Destroy(bo);
            m_BorderObjects.Clear();
        }

        m_BoardData = null;
        if (m_EmptyCellsList != null) m_EmptyCellsList.Clear();
    }
}
