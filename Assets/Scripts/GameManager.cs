using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}
    
    public BoardManager Board;

    public PlayerController Player;
    public TurnManager TurnManager {get; private set;}

    public UIDocument UIDoc;
    
    private Label m_FoodLabel;
    private Label m_DeathLabel;
    private int m_FoodAmount = 20;
    private DeathManager m_DeathManager;
    private int m_CurrentLevel = 1;
    private VisualElement m_GameOverPanel;
    private Label m_GameOverMessage;
    private bool m_IsGameActive = false;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        m_DeathManager = new DeathManager();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TurnManager = new TurnManager();
        TurnManager.OnTick += OnTurnHappen;
        NewLevel();
        
        m_GameOverPanel = UIDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
        m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");
        m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");
        m_DeathLabel = UIDoc.rootVisualElement.Q<Label>("DeathLabel");
        
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
        if (!m_IsGameActive) return;
        m_FoodAmount += amount;
        if (m_FoodAmount < 0) m_FoodAmount = 0;
        m_FoodLabel.text = "Food : " + m_FoodAmount;
        
        if (m_FoodAmount <= 0)
        {
            m_IsGameActive = false;
            Player.GameOver();
            m_GameOverPanel.style.visibility = Visibility.Visible;
            m_GameOverMessage.text = "Game Over!\n\nPress Enter for New Game\n\nYou traveled through " + m_CurrentLevel + " levels";
            HandlePlayerDeath();
        }
    }
    
    private void HandlePlayerDeath()
    {
        m_DeathManager.RegisterDeath();
        m_DeathLabel.text = "Deaths: " + m_DeathManager.TotalDeaths;
    }

    public void NewLevel()
    {
        Board.Clear();
        Board.Init();
        Player.Spawn(Board, new Vector2Int(1,1));
        
        m_CurrentLevel++;
    }

    public void StartNewGame()
    {
        m_GameOverPanel.style.visibility = Visibility.Hidden;
        m_IsGameActive = true;
        m_CurrentLevel = 1;
        m_FoodAmount = 20;
        m_FoodLabel.text = "Food : " + m_FoodAmount;
        m_DeathLabel.text = "Deaths: " + m_DeathManager.TotalDeaths;
        
        Board.Clear();
        Board.Init();
        
        Player.Init();
        Player.Spawn(Board, new Vector2Int(1,1));
    }

    
}
