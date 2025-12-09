using UnityEngine;

/// <summary>
/// CameraController flexible:
/// - Mode.FitMap: ajusta orthographicSize para que todo el mapa quepa en pantalla.
/// - Mode.FollowPlayer: mantiene el ortho size y sigue al jugador, limitando la cámara a los bordes del mapa.
/// 
/// Añadido: snapToGrid (si true, redondea la posición de la cámara a múltiplos de tileSize
/// para evitar medias celdas en pantalla).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public enum Mode { FitMap, FollowPlayer }
    public Mode mode = Mode.FitMap;

    public enum SnapMode { Round, Floor, Ceil }
    [Header("Referencias")]
    public BoardManager board;
    public Transform playerTransform; // asigna el transform del Player (o dejar null para buscarlo)
    public Camera cam;

    [Header("Parámetros")]
    public float tileSize = 1f; // unidades por tile (ajusta si tu Grid usa otra escala)
    public float paddingTiles = 1f; // margen en tiles
    public float minOrthoSize = 3f;
    public float maxOrthoSize = 100f;
    public float followSmoothTime = 0.05f;

    [Header("Snapping (posición de cámara)")]
    public bool snapToGrid = true;           // si true, la posición se redondea a múltiplos de tileSize
    public SnapMode snapMode = SnapMode.Round;

    // internal
    Vector3 velocity = Vector3.zero;

    void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (playerTransform == null)
        {
            var p = FindObjectOfType<PlayerController>();
            if (p != null) playerTransform = p.transform;
        }
    }

    void Start()
    {
        if (board == null) board = FindObjectOfType<BoardManager>();
        ApplyModeImmediate();
    }

    void LateUpdate()
    {
        if (board == null) return;

        if (mode == Mode.FitMap) FitMap();
        else FollowPlayer();
    }

    void ApplyModeImmediate()
    {
        if (board == null) return;
        if (mode == Mode.FitMap) FitMap();
        else FollowPlayer(true);
    }

    // ---- FitMap: centra y ajusta ortho ----
    void FitMap()
    {
        if (board == null) return;

        // Obtener world corners usando Board.CellToWorld para evitar offsets inesperados
        Vector3 minWorld = board.CellToWorld(new Vector2Int(0, 0));
        Vector3 maxWorld = board.CellToWorld(new Vector2Int(board.Width - 1, board.Height - 1));

        float mapWidthUnits = Mathf.Abs(maxWorld.x - minWorld.x) + tileSize;
        float mapHeightUnits = Mathf.Abs(maxWorld.y - minWorld.y) + tileSize;

        float aspect = (float)Screen.width / (float)Screen.height;
        float pad = paddingTiles * tileSize;

        float halfHeightNeeded = (mapHeightUnits / 2f) + pad;
        float halfWidthNeeded = (mapWidthUnits / 2f) / aspect + pad;

        float targetOrtho = Mathf.Max(halfHeightNeeded, halfWidthNeeded, minOrthoSize);
        targetOrtho = Mathf.Clamp(targetOrtho, minOrthoSize, maxOrthoSize);

        cam.orthographic = true;
        cam.orthographicSize = targetOrtho;

        // Centrar la cámara en el centro real del área world
        float centerX = (minWorld.x + maxWorld.x) / 2f;
        float centerY = (minWorld.y + maxWorld.y) / 2f;
        Vector3 targetPos = new Vector3(centerX, centerY, transform.position.z);

        // Snap opcional a grid
        transform.position = SnapPosition(targetPos);
    }

    // ---- FollowPlayer: sigue al jugador y aplica límites ----
    void FollowPlayer(bool snap = false)
    {
        if (playerTransform == null)
        {
            FitMap();
            return;
        }

        cam.orthographic = true;

        Vector3 target = new Vector3(playerTransform.position.x, playerTransform.position.y, transform.position.z);

        // calcular límites en world usando Board.CellToWorld
        Vector3 minWorld = board.CellToWorld(new Vector2Int(0, 0));
        Vector3 maxWorld = board.CellToWorld(new Vector2Int(board.Width - 1, board.Height - 1));
        float halfTile = tileSize / 2f;

        float mapMinX = minWorld.x - halfTile;
        float mapMinY = minWorld.y - halfTile;
        float mapMaxX = maxWorld.x + halfTile;
        float mapMaxY = maxWorld.y + halfTile;

        float vertExtent = cam.orthographicSize;
        float horzExtent = cam.orthographicSize * ((float)Screen.width / (float)Screen.height);

        float minX = mapMinX + horzExtent;
        float maxX = mapMaxX - horzExtent;
        float minY = mapMinY + vertExtent;
        float maxY = mapMaxY - vertExtent;

        if (minX > maxX) { minX = maxX = (mapMinX + mapMaxX) / 2f; }
        if (minY > maxY) { minY = maxY = (mapMinY + mapMaxY) / 2f; }

        float clampedX = Mathf.Clamp(target.x, minX, maxX);
        float clampedY = Mathf.Clamp(target.y, minY, maxY);
        Vector3 desired = new Vector3(clampedX, clampedY, transform.position.z);

        if (snap)
        {
            transform.position = SnapPosition(desired);
            return;
        }

        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity, followSmoothTime);

        // aplicamos snap tras suavizado para evitar medias celdas
        if (snapToGrid)
            transform.position = SnapPosition(smoothed);
        else
            transform.position = smoothed;
    }

    // Snap a múltiplos de tileSize según snapMode
    Vector3 SnapPosition(Vector3 worldPos)
    {
        if (!snapToGrid || tileSize <= 0f) return new Vector3(worldPos.x, worldPos.y, worldPos.z);

        float x = worldPos.x / tileSize;
        float y = worldPos.y / tileSize;

        switch (snapMode)
        {
            case SnapMode.Round:
                x = Mathf.Round(x);
                y = Mathf.Round(y);
                break;
            case SnapMode.Floor:
                x = Mathf.Floor(x);
                y = Mathf.Floor(y);
                break;
            case SnapMode.Ceil:
                x = Mathf.Ceil(x);
                y = Mathf.Ceil(y);
                break;
        }

        return new Vector3(x * tileSize, y * tileSize, worldPos.z);
    }

    // Método público para forzar recálculo (llámalo después de InitLevel)
    public void RefreshCamera()
    {
        ApplyModeImmediate();
    }
}
