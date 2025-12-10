using System;
using UnityEngine;

/// <summary>
/// Cámara que sigue al jugador con lookahead en tiles:
/// - Asigna playerTransform y, opcionalmente, confineCollider (BoxCollider2D) o BoardManager para límites.
/// - lookaheadTiles: cuántas celdas hacia delante quiere mostrar cuando el jugador se mueve en una dirección.
/// - snapToGrid evita medias celdas (tileSize por defecto = 1).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform playerTransform;       // arrastra el Player (transform)
    public BoxCollider2D confineCollider;   // opcional: area de confinamiento (en world coords)
    public BoardManager board;              // opcional: si no hay confineCollider, usamos board.CellToWorld bounds

    [Header("Parámetros")]
    public float tileSize = 1f;
    public bool snapToGrid = true;
    public float followSmoothTime = 0.06f;

    [Header("Lookahead")]
    [Tooltip("Número de tiles que la cámara debe adelantarse en la dirección del movimiento")]
    public float lookaheadTiles = 2f;
    [Tooltip("Lerp para suavizar el cambio de lookahead (0..1). 0 = snap instantáneo, 1 = sin smoothing extra")]
    [Range(0f, 1f)] public float lookaheadSmoothing = 0.25f;

    private Camera m_Cam;
    private Vector3 m_Velocity = Vector3.zero;
    private Vector2 m_LastMoveDir = Vector2.up; // dirección utilizada cuando el jugador está parado
    private Vector3 m_PreviousPlayerPos;

    [Obsolete("Obsolete")]
    void Awake()
    {
        m_Cam = GetComponent<Camera>();
        if (playerTransform == null)
        {
            var p = FindObjectOfType<PlayerController>();
            if (p != null) playerTransform = p.transform;
        }
        if (board == null) board = FindObjectOfType<BoardManager>();
        m_PreviousPlayerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // 1) calcular dirección de movimiento basada en delta posición del player
        Vector3 playerPos = playerTransform.position;
        Vector3 delta = playerPos - m_PreviousPlayerPos;

        Vector2 moveDir = m_LastMoveDir;
        if (delta.magnitude > 0.001f)
        {
            moveDir = new Vector2(delta.x, delta.y).normalized;
            m_LastMoveDir = moveDir;
        }

        m_PreviousPlayerPos = playerPos;

        // 2) calcular objetivo con lookahead (en world units)
        Vector3 lookOffset = new Vector3(moveDir.x, moveDir.y, 0f) * (lookaheadTiles * tileSize);

        // suavizar transiciones de lookahead para evitar saltos
        Vector3 currentOffset = Vector3.Lerp(Vector3.zero, lookOffset, 1f - lookaheadSmoothing);

        Vector3 desiredWorld = new Vector3(playerPos.x, playerPos.y, transform.position.z) + currentOffset;

        // 3) obtener bounds de confinamiento (world)
        Bounds confineBounds;
        bool haveBounds = false;

        if (confineCollider != null)
        {
            confineBounds = confineCollider.bounds;
            haveBounds = true;
        }
        else if (board != null)
        {
            // usamos las esquinas world del board (note: CellToWorld devuelve centros)
            Vector3 minWorld = board.CellToWorld(new Vector2Int(0, 0));
            Vector3 maxWorld = board.CellToWorld(new Vector2Int(board.Width - 1, board.Height - 1));
            Vector3 center = (minWorld + maxWorld) / 2f;
            Vector3 size = new Vector3(Mathf.Abs(maxWorld.x - minWorld.x) + tileSize, Mathf.Abs(maxWorld.y - minWorld.y) + tileSize, 1f);
            confineBounds = new Bounds(center, size);
            haveBounds = true;
        }
        else
        {
            haveBounds = false;
            confineBounds = new Bounds(Vector3.zero, Vector3.zero);
        }

        // 4) calcular extents de la cámara
        float vertExt = m_Cam.orthographicSize;
        float horzExt = m_Cam.orthographicSize * ((float)Screen.width / Screen.height);

        // 5) expandir temporalmente bounds en la dirección del movimiento para permitir ver lookaheadTiles
        float extra = lookaheadTiles * tileSize;
        float minX = confineBounds.min.x + horzExt;
        float maxX = confineBounds.max.x - horzExt;
        float minY = confineBounds.min.y + vertExt;
        float maxY = confineBounds.max.y - vertExt;

        if (haveBounds)
        {
            // expandir solo en la dirección del movimiento
            if (moveDir.x > 0.01f) maxX += extra;
            else if (moveDir.x < -0.01f) minX -= extra;

            if (moveDir.y > 0.01f) maxY += extra;
            else if (moveDir.y < -0.01f) minY -= extra;

            // si el area es más pequeña que la vista, centra en el bounds center
            if (minX > maxX) { minX = maxX = confineBounds.center.x; }
            if (minY > maxY) { minY = maxY = confineBounds.center.y; }

            // clamp desired
            desiredWorld.x = Mathf.Clamp(desiredWorld.x, minX, maxX);
            desiredWorld.y = Mathf.Clamp(desiredWorld.y, minY, maxY);
        }

        // 6) Smooth follow hacia desiredWorld
        Vector3 smoothPos = Vector3.SmoothDamp(transform.position, new Vector3(desiredWorld.x, desiredWorld.y, transform.position.z), ref m_Velocity, followSmoothTime);

        // 7) Snap a grid si requerido
        if (snapToGrid && tileSize > 0f)
        {
            smoothPos.x = Mathf.Round(smoothPos.x / tileSize) * tileSize;
            smoothPos.y = Mathf.Round(smoothPos.y / tileSize) * tileSize;
        }

        transform.position = smoothPos;
    }

    // Método público para forzar snap inmediato al player (útil al generar nivel)
    public void RefreshImmediately()
    {
        if (playerTransform == null) return;
        Vector3 pos = playerTransform.position;
        if (snapToGrid && tileSize > 0f)
        {
            pos.x = Mathf.Round(pos.x / tileSize) * tileSize;
            pos.y = Mathf.Round(pos.y / tileSize) * tileSize;
        }
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }
}
