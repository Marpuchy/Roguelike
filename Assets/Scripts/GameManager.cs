using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}
    
    [FormerlySerializedAs("Board")] public BoardManager board;

    [FormerlySerializedAs("Player")] public PlayerController player;
    public TurnManager TurnManager {get; private set;}

    [FormerlySerializedAs("UIDoc")] public UIDocument uiDoc;
    
    private Label m_FoodLabel;
    private int m_FoodAmount = 20;
    private int m_CurrentLevel = 1;
    private VisualElement m_GameOverPanel;
    private Label m_GameOverMessage;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TurnManager = new TurnManager();
        TurnManager.OnTick += OnTurnHappen;

        // Nota: mantenemos la llamada a NewLevel si tu flujo la requiere.
        NewLevel();
        
        m_GameOverPanel = uiDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
        m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");
        m_FoodLabel = uiDoc.rootVisualElement.Q<Label>("FoodLabel");
        
        StartNewGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTurnHappen()
    {
        ChangeFood(-1);
    }
    
    public void ChangeFood(int amount)
    {
        m_FoodAmount += amount;
        if (m_FoodLabel != null) m_FoodLabel.text = "Food : " + m_FoodAmount;

        if (m_FoodAmount <= 0)
        {
            player.GameOver();
            if (m_GameOverPanel != null) m_GameOverPanel.style.visibility = Visibility.Visible;
            if (m_GameOverMessage != null) m_GameOverMessage.text = "Game Over!\n\nYou traveled through " + m_CurrentLevel + " levels";
        }
    }

    public void NewLevel()
    {
        board.Clear();
        board.InitLevel(m_CurrentLevel);
        player.Spawn(board, new Vector2Int(1,1));
        m_CurrentLevel++;
    }

    public void StartNewGame()
    {
        if (m_GameOverPanel != null) m_GameOverPanel.style.visibility = Visibility.Hidden;
        
        m_CurrentLevel = 1;
        m_FoodAmount = 20;
        if (m_FoodLabel != null) m_FoodLabel.text = "Food : " + m_FoodAmount;
        
        board.Clear();

        // inicializa el primer nivel con tamaño basado en m_CurrentLevel (1 -> 8x8)
        board.InitLevel(m_CurrentLevel);
        
        player.Init();
        player.Spawn(board, new Vector2Int(1,1));
    }
}
