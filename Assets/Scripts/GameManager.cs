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
        NewLevel();
        
        m_GameOverPanel = UIDoc.rootVisualElement.Q<VisualElement>("GameOverPanel");
        m_GameOverMessage = m_GameOverPanel.Q<Label>("GameOverMessage");
        m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");
        
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
        m_FoodLabel.text = "Food : " + m_FoodAmount;

        if (m_FoodAmount <= 0)
        {
            Player.GameOver();
            m_GameOverPanel.style.visibility = Visibility.Visible;
            m_GameOverMessage.text = "Game Over!\n\nYou traveled through " + m_CurrentLevel + " levels";
        }
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
        
        m_CurrentLevel = 1;
        m_FoodAmount = 20;
        m_FoodLabel.text = "Food : " + m_FoodAmount;
        
        Board.Clear();
        Board.Init();
        
        Player.Init();
        Player.Spawn(Board, new Vector2Int(1,1));
    }

    
}
