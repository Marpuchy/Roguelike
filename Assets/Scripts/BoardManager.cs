using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// BoardManager mejorado:
/// - Soporta inicializar tamaño por nivel (baseSize + (level-1))
/// - Escala food/enemies/walls en función del área/nivel
/// - Protecciones contra listas vacías y limites
/// - Métodos públicos InitLevel(level) y Init(width,height)
/// - Opciones configurables desde el Inspector para el escalado de enemigos
/// </summary>
public class BoardManager : MonoBehaviour
{
    private Tilemap m_Tilemap;
    private Grid m_Grid;
    private CellData[,] m_BoardData;
    private List<Vector2Int> m_EmptyCellsList;

    [Header("Tamaño (si llamas InitLevel)")]
    public int baseSize = 8;       // nivel 1 -> 8x8
    public int maxSize = 200;

    [Header("Tamaño actual (seteado por Init/InitLevel)")]
    public int Width;
    public int Height;

    [Header("Tiles / Prefabs")]
    public Tile[] GroundTiles;
    public Tile[] WallTiles;
    public FoodObject[] FoodPrefabs;
    public int minFoodCount = 1;
    public int maxFoodCount = 3;
    public WallObject[] WallPrefabs;
    public ExitCellObject ExitCellPrefab;
    public Enemy EnemyPrefab;

    public PlayerController Player;

    [Header("Opciones de escalado de enemigos")]
    [Tooltip("Si está activado, calcula enemigos como (area/cellsPerEnemy) * (levelMultiplier * CurrentLevel) + baseEnemies")]
    public bool useAreaPerLevelScaling = true;

    [Tooltip("Número de casillas por cada enemigo base (ej. 40 => 1 enemigo por cada 40 casillas)")]
    public int cellsPerEnemy = 40;

    [Tooltip("Multiplicador por nivel (ej. 0.5 => 0.5 * level)")]
    public float levelMultiplier = 0.5f;

    [Tooltip("Enemigos base añadidos al cálculo")]
    public int baseEnemies = 0;

    [Tooltip("Si true redondea hacia arriba en lugar de hacia abajo")]
    public bool roundUpEnemies = false;

    [Tooltip("Cap máximo de enemigos (0 = sin cap salvo celdas libres)")]
    public int maxEnemyCap = 0;

    // Propiedad para saber el nivel actual (seteada por InitLevel)
    public int CurrentLevel { get; private set; } = 1;

    /// <summary>
    /// Inicializa el tablero usando level -> calcula tamaño (baseSize + level-1)
    /// </summary>
    public void InitLevel(int level)
    {
        // guardar el nivel actual para usar en GenerateEnemy
        CurrentLevel = Mathf.Max(1, level);

        int size = baseSize + Mathf.Max(0, CurrentLevel - 1);
        size = Mathf.Clamp(size, 3, maxSize); // mínimo 3x3 por seguridad
        Init(size, size);
    }

    /// <summary>
    /// Inicializa el tablero con width x height directamente
    /// </summary>
    public void Init(int width, int height)
    {
        Width = Mathf.Clamp(width, 3, maxSize);
        Height = Mathf.Clamp(height, 3, maxSize);

        if (m_Tilemap == null) m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Grid == null) m_Grid = GetComponentInChildren<Grid>();

        // limpiar posible contenido previo
        ClearInternal();

        m_BoardData = new CellData[Width, Height];
        m_EmptyCellsList = new List<Vector2Int>();

        // crear tiles y board data
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Tile tile;
                m_BoardData[x, y] = new CellData();
                if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                {
                    // borde -> muro
                    tile = (WallTiles != null && WallTiles.Length > 0) ? WallTiles[Random.Range(0, WallTiles.Length)] : null;
                    m_BoardData[x, y].Passable = false;
                }
                else
                {
                    tile = (GroundTiles != null && GroundTiles.Length > 0) ? GroundTiles[Random.Range(0, GroundTiles.Length)] : null;
                    m_BoardData[x, y].Passable = true;
                    m_EmptyCellsList.Add(new Vector2Int(x, y));
                }

                if (m_Tilemap != null)
                    m_Tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        // Evitar spawn en 1,1 (si existe) para mantener compatibilidad con tu lógica previa
        m_EmptyCellsList.Remove(new Vector2Int(1, 1));

        // Spawn exit en la esquina opuesta (Width-2, Height-2) si está libre
        Vector2Int endCoord = new Vector2Int(Mathf.Max(1, Width - 2), Mathf.Max(1, Height - 2));
        if (TryReserveCell(endCoord))
        {
            if (ExitCellPrefab != null)
            {
                AddObject(Instantiate(ExitCellPrefab), endCoord);
            }
        }
        else
        {
            // si no estaba libre, intenta colocar en la primera celda libre
            if (m_EmptyCellsList.Count > 0)
            {
                Vector2Int fallback = m_EmptyCellsList[0];
                if (ExitCellPrefab != null) AddObject(Instantiate(ExitCellPrefab), fallback);
                m_EmptyCellsList.RemoveAt(0);
            }
        }

        GenerateWall();
        GenerateFood();
        GenerateEnemy();
    }

    // ---- helpers ----

    private bool TryReserveCell(Vector2Int coord)
    {
        if (m_EmptyCellsList == null) return false;
        // si la coord está en la lista (Contains lo compara bien para Vector2Int)
        if (m_EmptyCellsList.Contains(coord))
        {
            m_EmptyCellsList.Remove(coord);
            return true;
        }
        return false;
    }

    // Clear sólo de datos internos antes de re-init (no destruye tilemap children externos)
    private void ClearInternal()
    {
        if (m_BoardData != null)
        {
            for (int y = 0; y < m_BoardData.GetLength(1); y++)
            {
                for (int x = 0; x < m_BoardData.GetLength(0); x++)
                {
                    var cellData = m_BoardData[x, y];
                    if (cellData?.ContainedObject != null)
                    {
                        Destroy(cellData.ContainedObject.gameObject);
                    }
                    if (m_Tilemap != null)
                        m_Tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }
    }

    // Update no se usa aquí (dejado intencionalmente vacío)
    void Update() { }

    public Vector3 CellToWorld(Vector2Int cellIndex)
    {
        return m_Grid.GetCellCenterWorld((Vector3Int)cellIndex);
    }

    public CellData CellData(Vector2Int cellIndex)
    {
        if (cellIndex.x < 0 || cellIndex.x >= Width
                          || cellIndex.y < 0 || cellIndex.y >= Height)
        {
            return null;
        }

        return m_BoardData[cellIndex.x, cellIndex.y];
    }

    void AddObject(CellObject obj, Vector2Int coord)
    {
        if (coord.x < 0 || coord.x >= Width || coord.y < 0 || coord.y >= Height) return;
        CellData data = m_BoardData[coord.x, coord.y];
        if (data == null) return;
        obj.transform.position = CellToWorld(coord);
        data.ContainedObject = obj;
        obj.Init(coord);
    }

    // ---- Generadores (escalado) ----

    void GenerateFood()
    {
        if (FoodPrefabs == null || FoodPrefabs.Length == 0 || m_EmptyCellsList == null) return;

        // escala por área
        int area = Width * Height;
        int baseFood = Random.Range(minFoodCount, maxFoodCount + 1);
        int scaled = Mathf.Clamp(baseFood + area / 30, minFoodCount, Mathf.Max(maxFoodCount, baseFood + area / 20));

        int foodCount = Mathf.Min(scaled, m_EmptyCellsList.Count);

        for (int i = 0; i < foodCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            m_EmptyCellsList.RemoveAt(randomIndex);
            FoodObject food = FoodPrefabs[Random.Range(0, FoodPrefabs.Length)];
            FoodObject newFood = Instantiate(food);
            AddObject(newFood, coord);
        }
    }

    void GenerateEnemy()
    {
        if (EnemyPrefab == null || m_EmptyCellsList == null || m_EmptyCellsList.Count == 0) return;

        int area = Width * Height;
        int enemyCount = 0;

        if (useAreaPerLevelScaling)
        {
            // cálculo básico: (area / cellsPerEnemy) * (levelMultiplier * CurrentLevel) + baseEnemies
            if (cellsPerEnemy <= 0) cellsPerEnemy = 40; // seguridad

            float enemiesFromArea = (float)area / (float)cellsPerEnemy;
            float scaled = enemiesFromArea * (levelMultiplier * (float)CurrentLevel);
            float suggestedFloat = (float)baseEnemies + scaled;

            if (roundUpEnemies)
                enemyCount = Mathf.CeilToInt(suggestedFloat);
            else
                enemyCount = Mathf.FloorToInt(suggestedFloat);

            // asegurar no negativo
            enemyCount = Mathf.Max(0, enemyCount);

            // aplicar cap si se especificó (maxEnemyCap == 0 => no cap por esta variable)
            if (maxEnemyCap > 0) enemyCount = Mathf.Min(enemyCount, maxEnemyCap);
        }
        else
        {
            // fallback: método previo reducido (compatible)
            int baseE = Random.Range(1, 2);
            int scaledFromArea = Mathf.FloorToInt(area / 80f);
            int suggested = baseE + scaledFromArea;
            int cap = Mathf.Max(1, Mathf.FloorToInt(area / 30f));
            enemyCount = Mathf.Clamp(suggested, 0, cap);
        }

        // no podemos crear más enemigos que celdas libres
        enemyCount = Mathf.Clamp(enemyCount, 0, m_EmptyCellsList.Count);

        for (int i = 0; i < enemyCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            m_EmptyCellsList.RemoveAt(randomIndex);
            Enemy newEnemy = Instantiate(EnemyPrefab);
            AddObject(newEnemy, coord);
        }
    }


    void GenerateWall()
    {
        if (WallPrefabs == null || WallPrefabs.Length == 0 || m_EmptyCellsList == null) return;

        int area = Width * Height;
        int baseWalls = Random.Range(6, 10);
        int scaled = Mathf.Clamp(baseWalls + area / 50, 0, Mathf.Max(10, area / 8));

        int wallCount = Mathf.Min(scaled, m_EmptyCellsList.Count);

        for (int i = 0; i < wallCount; ++i)
        {
            if (m_EmptyCellsList.Count == 0) break;
            int randomIndex = Random.Range(0, m_EmptyCellsList.Count);
            Vector2Int coord = m_EmptyCellsList[randomIndex];

            m_EmptyCellsList.RemoveAt(randomIndex);
            WallObject wall = WallPrefabs[Random.Range(0, WallPrefabs.Length)];
            WallObject newWall = Instantiate(wall);
            AddObject(newWall, coord);
        }
    }

    public void SetCellTile(Vector2Int cellIndex, Tile tile)
    {
        if (m_Tilemap == null) return;
        m_Tilemap.SetTile(new Vector3Int(cellIndex.x, cellIndex.y, 0), tile);
    }

    public Tile GetCelltile(Vector2Int cellIndex)
    {
        if (m_Tilemap == null) return null;
        return m_Tilemap.GetTile<Tile>(new Vector3Int(cellIndex.x, cellIndex.y, 0));
    }

    public void Clear()
    {
        if (m_BoardData == null) return;

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                var cellData = m_BoardData[x, y];
                if (cellData != null && cellData.ContainedObject != null)
                {
                    Destroy(cellData.ContainedObject.gameObject);
                }
                if (m_Tilemap != null)
                    SetCellTile(new Vector2Int(x, y), null);
            }
        }

        m_BoardData = null;
        if (m_EmptyCellsList != null) m_EmptyCellsList.Clear();
    }
}
