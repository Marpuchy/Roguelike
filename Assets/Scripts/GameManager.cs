using UnityEngine;

public class GameManager : MonoBehaviour
{

    public BoardManager Board;

    public PlayerController Player;

    private TurnManager m_turnManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_turnManager = new TurnManager();
        Board.Init();
        Player.Spawn(Board, new Vector2Int(1,1));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
