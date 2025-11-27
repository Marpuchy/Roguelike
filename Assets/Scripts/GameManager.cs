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

    private int m_FoodAmount = 100;
    
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
        Board.Init();
        Player.Spawn(Board, new Vector2Int(1,1));
        m_FoodLabel = UIDoc.rootVisualElement.Q<Label>("FoodLabel");
        m_FoodLabel.text = "Food : " + m_FoodAmount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTurnHappen()
    {
        m_FoodAmount--;
        m_FoodLabel.text = "Food : " + m_FoodAmount;
    }
}
