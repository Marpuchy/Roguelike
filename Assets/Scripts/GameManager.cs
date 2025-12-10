using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [FormerlySerializedAs("Board")] public BoardManager board;
    [FormerlySerializedAs("Player")] public PlayerController player;
    public TurnManager TurnManager { get; private set; }

    [FormerlySerializedAs("UIDoc")] public UIDocument uiDoc;
    
    // UI Toolkit label (preferida)
    private Label m_FoodLabel;
    // Fallback UI (UnityEngine.UI) si no hay UIDocument / Label
    private Text m_FoodTextFallback;

    private int m_FoodAmount = 20;
    private int m_CurrentLevel = 1;
    private VisualElement m_GameOverPanel;
    private Label m_GameOverMessage;

    // impide que ticks tempranos afecten al estado
    private bool initialized = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // aseguramos un valor por defecto
        m_FoodAmount = 20;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // inicializamos TurnManager y nos suscribimos
        TurnManager = new TurnManager();
        TurnManager.OnTick += OnTurnHappen;

        // inicializamos UI (UI Toolkit si hay uiDoc)
        InitializeUI();

        // empezamos el juego con el flujo existente, pero protegidos contra ticks tempranos
        initialized = false; // bloquea OnTurnHappen hasta que StartNewGame termine
        NewLevel(); // mantengo tu llamada original por compatibilidad con tu flujo
        StartNewGame();
        initialized = true;
    }

    void InitializeUI()
    {
        // Si hay UIDocument, intentar obtener elementos por nombre
        if (uiDoc != null)
        {
            try
            {
                m_GameOverPanel = uiDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
                if (m_GameOverPanel != null)
                    m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");
                m_FoodLabel = uiDoc.rootVisualElement.Q<Label>("FoodLabel");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameManager] Error leyendo UIDocument: " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] uiDoc no asignado en el inspector. Usaré un fallback UI Text.");
        }

        // Si no encontramos la FoodLabel de UI Toolkit, creamos un fallback sencillo con Canvas + Text
        if (m_FoodLabel == null)
        {
            CreateFallbackFoodText();
            Debug.Log("[GameManager] FoodLabel no encontrada -> creado fallback Text en Canvas.");
        }
    }

    void CreateFallbackFoodText()
    {
        // intenta reutilizar un Canvas existente
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("RuntimeCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // crear GameObject Text
        GameObject txtGO = new GameObject("FoodTextFallback");
        txtGO.transform.SetParent(canvas.transform, false);
        m_FoodTextFallback = txtGO.AddComponent<Text>();
        m_FoodTextFallback.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        m_FoodTextFallback.fontSize = 18;
        m_FoodTextFallback.alignment = TextAnchor.UpperLeft;
        m_FoodTextFallback.color = Color.black;
        m_FoodTextFallback.rectTransform.anchorMin = new Vector2(0, 1);
        m_FoodTextFallback.rectTransform.anchorMax = new Vector2(0, 1);
        m_FoodTextFallback.rectTransform.pivot = new Vector2(0, 1);
        m_FoodTextFallback.rectTransform.anchoredPosition = new Vector2(10, -10);

        // inicial text
        m_FoodTextFallback.text = "Food : " + m_FoodAmount;
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnTurnHappen()
    {
        // Ignorar ticks hasta que la inicialización haya finalizado
        if (!initialized) return;
        ChangeFood(-1);
    }
    
    public void ChangeFood(int amount)
    {
        // Seguridad: si aún no estamos completamente inicializados, ignorar cambios accidentales
        if (!initialized)
        {
            Debug.Log("[GameManager] ChangeFood ignorado porque game no está inicializado todavía.");
            return;
        }

        m_FoodAmount += amount;

        // actualizar UI Toolkit si existe
        if (m_FoodLabel != null)
        {
            try
            {
                m_FoodLabel.text = "Food : " + m_FoodAmount;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameManager] error actualizando m_FoodLabel: " + ex.Message);
            }
        }

        // actualizar fallback UI Text si existe
        if (m_FoodTextFallback != null)
        {
            m_FoodTextFallback.text = "Food : " + m_FoodAmount;
        }

        // comprobaciones GameOver
        if (m_FoodAmount <= 0)
        {
            if (player != null) player.GameOver();
            if (m_GameOverPanel != null) m_GameOverPanel.style.visibility = Visibility.Visible;
            if (m_GameOverMessage != null) m_GameOverMessage.text = "Game Over!\n\nYou traveled through " + m_CurrentLevel + " levels";
            // Si no hay UI Toolkit, como fallback mostramos por consola
            if (m_GameOverPanel == null)
                Debug.Log("[GameManager] Game Over! (no GameOverPanel encontrado)");
        }
    }

    public void NewLevel()
    {
        // Subimos el nivel primero para que BoardManager reciba el nuevo número
        m_CurrentLevel++;
        Debug.Log($"[GameManager] NewLevel -> level={m_CurrentLevel}");

        if (board != null)
        {
            board.Clear();
            board.InitLevel(m_CurrentLevel); // BoardManager usará level para tamaño
        }
        else
        {
            Debug.LogWarning("[GameManager] board is null on NewLevel()");
        }

        if (player != null) player.Spawn(board, new Vector2Int(1,1));
    }


    public void StartNewGame()
    {
        // Ocultar GameOver si hay panel
        if (m_GameOverPanel != null) m_GameOverPanel.style.visibility = Visibility.Hidden;

        m_CurrentLevel = 1;
        m_FoodAmount = 20;

        // actualizar UI (ambos tipos)
        if (m_FoodLabel != null) m_FoodLabel.text = "Food : " + m_FoodAmount;
        if (m_FoodTextFallback != null) m_FoodTextFallback.text = "Food : " + m_FoodAmount;

        if (board != null)
        {
            board.Clear();
            board.Init();
        }
        else
        {
            Debug.LogWarning("[GameManager] board no asignado en inspector en StartNewGame().");
        }
        
        if (player != null)
        {
            player.Init();
            player.Spawn(board, new Vector2Int(1,1));
        }
    }
}
 