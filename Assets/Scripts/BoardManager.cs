using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// BoardManager compatible:
/// - InitLevel(level) interpreta baseSize + (level-1) como total visible (si createOuterBorder true, interior = total - 2*borderThickness)
/// - Crea interior jugable (Width x Height), coloca Player en (0,0) y Exit en (Width-1, Height-1)
/// - Busca/crea SimpleSpawner (Roguelike.Spawning.SimpleSpawner), copia configs desde el Inspector y llama SpawnAll(...)
/// - Mantiene firmas públicas previas para compatibilidad con otros scripts
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("Tamaño")]
    public int baseSize = 8;    // total visible size for level 1
    public int maxSize = 200;

    [Header("Border (visual)")]
    public bool createOuterBorder = true;
    [Range(1, 5)] public int borderThickness = 1;
    public bool instantiateBorderWallObjects = false;

    [Header("Tiles / Prefabs")]
    public Tile[] GroundTiles;
    public Tile[] WallTiles; // solo para border visual (tile)
    public ExitCellObject ExitCellPrefab;

    [Header("Spawner configs (edítalas aquí)")]
    public SimpleSpawner.FoodConfig spawnFood = new SimpleSpawner.FoodConfig();
    public SimpleSpawner.WallConfig spawnWalls = new SimpleSpawner.WallConfig();
    public SimpleSpawner.EnemyConfig spawnEnemies = new SimpleSpawner.EnemyConfig();

    [Header("Player (opcional - si no está asignado se buscará en la escena)")]
    public PlayerController Player; // no cambiaremos PlayerController; solo llamamos Player.Spawn(...)

    // Public read-only interior logical size
    public int Width { get; private set; }
    public int Height { get; private set; }

    // internals
    Tilemap m_Tilemap;
    Grid m_Grid;
    CellData[,] m_BoardData;
    List<Vector2Int> m_EmptyCellsList;
    List<GameObject> m_BorderObjects;

    // runtime spawner (namespaced)
    SimpleSpawner m_Spawner;

    void Awake()
    {
        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Grid == null) m_Grid = GetComponentInChildren<Grid>();

        // intentar obtener player automáticamente si no asignado en el inspector
        if (Player == null)
        {
            Player = FindObjectOfType<PlayerController>();
        }

        // encontrar o crear SimpleSpawner namespaced automáticamente
        m_Spawner = FindObjectOfType<SimpleSpawner>();
        if (m_Spawner == null)
        {
            GameObject go = new GameObject("SimpleSpawner");
            m_Spawner = go.AddComponent<SimpleSpawner>();
            Debug.Log("[BoardManager] SimpleSpawner creado automáticamente.");
        }
    }

    // Backwards-compatible Init()
    public void Init() => InitLevel(1);

    /// <summary>
    /// Interpreta totalSize = baseSize + (level-1) como tamaño TOTAL visible.
    /// Si createOuterBorder = true, interiorSize = totalSize - 2*borderThickness.
    /// </summary>
    public void InitLevel(int level)
    {
        int totalSize = baseSize + Mathf.Max(0, level - 1);
        totalSize = Mathf.Clamp(totalSize, 3, maxSize);

        int interiorSize = createOuterBorder ? totalSize - 2 * borderThickness : totalSize;
        interiorSize = Mathf.Clamp(interiorSize, 3, totalSize);

        Init(interiorSize, interiorSize);
    }

    /// <summary>
    /// Inicializa el tablero interior lógico Width x Height
    /// </summary>
    public void Init(int width, int height)
    {
        Width = Mathf.Clamp(width, 3, maxSize);
        Height = Mathf.Clamp(height, 3, maxSize);

        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Grid == null) m_Grid = GetComponentInChildren<Grid>();

        ClearInternal();

        m_BoardData = new CellData[Width, Height];
        m_EmptyCellsList = new List<Vector2Int>();
        m_BorderObjects = new List<GameObject>();

        // Fill interior with ground tiles and collect empty cells
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                m_BoardData[x, y] = new CellData { Passable = true };
                m_EmptyCellsList.Add(new Vector2Int(x, y));

                Tile ground = (GroundTiles != null && GroundTiles.Length > 0) ? GroundTiles[Random.Range(0, GroundTiles.Length)] : null;
                if (m_Tilemap != null) m_Tilemap.SetTile(new Vector3Int(x, y, 0), ground);
            }
        }

        // Player bottom-left (0,0) -- mantenemos la llamada original a Player.Spawn(...)
        Vector2Int playerPos = new Vector2Int(0, 0);
        if (Player == null) Player = FindObjectOfType<PlayerController>();
        if (Player != null)
        {
            if (m_EmptyCellsList.Contains(playerPos)) m_EmptyCellsList.Remove(playerPos);
            // NO MODIFICAMOS PlayerController; solo llamamos la firma que ya existía
            Player.Spawn(this, playerPos);
        }

        // Exit top-right (Width-1, Height-1)
        Vector2Int exitPos = new Vector2Int(Width - 1, Height - 1);
        if (m_EmptyCellsList.Contains(exitPos)) m_EmptyCellsList.Remove(exitPos);
        if (ExitCellPrefab != null) AddObject(Instantiate(ExitCellPrefab), exitPos);

        // Border visual (dibujado fuera del interior lógico)
        if (createOuterBorder)
        {
            Tile wallTile = (WallTiles != null && WallTiles.Length > 0) ? WallTiles[0] : null;
            GameObject wallPrefab = (instantiateBorderWallObjects && m_Spawner != null && m_Spawner.WallPrefabs != null && m_Spawner.WallPrefabs.Length > 0) ? m_Spawner.WallPrefabs[0].gameObject : null;
            BorderSpawner.CreateOuterBorder(m_Tilemap, wallTile, Width, Height, borderThickness, wallPrefab, this.transform);
        }

        // PASAR configs editadas en este BoardManager al SimpleSpawner (copy-by-value)
        if (m_Spawner != null)
        {
            m_Spawner.food = spawnFood;
            m_Spawner.walls = spawnWalls;
            m_Spawner.enemies = spawnEnemies;
        }

        // Delegar spawn al SimpleSpawner (consume m_EmptyCellsList)
        if (m_Spawner != null)
        {
            m_Spawner.SpawnAll(m_EmptyCellsList, this);
        }
        else
        {
            Debug.LogWarning("[BoardManager] No SimpleSpawner disponible - no se spawneará nada.");
        }
    }

    // ----------------- API helpers (compatibles) -----------------

    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        if (m_Grid == null) m_Grid = GetComponentInChildren<Grid>();
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    public CellData CellData(Vector2Int cellIndex)
    {
        if (cellIndex.x < 0 || cellIndex.x >= Width || cellIndex.y < 0 || cellIndex.y >= Height) return null;
        return m_BoardData[cellIndex.x, cellIndex.y];
    }

    // Firma idéntica: AddObject(CellObject, Vector2Int)
    public void AddObject(CellObject obj, Vector2Int coord)
    {
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

    // Public Clear: destruye objetos lógicos y limpia tiles interiores
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

        if (m_BorderObjects != null)
        {
            foreach (var go in m_BorderObjects) if (go != null) Destroy(go);
            m_BorderObjects.Clear();
        }
    }

    // Limpieza interna previa a Init (no destruye tilemap children externos)
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
            // usamos Width/Height actuales (si existían)
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
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
    }
}
