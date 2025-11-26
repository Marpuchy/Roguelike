using DefaultNamespace;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    private BoardManager m_Board;

    private Vector2Int m_CellPosition;

    private InputAction _inputAction;

    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        m_Board = boardManager;
        MoveTo(cell);
    }

    public void MoveTo(Vector2Int cell)
    {
        m_CellPosition = cell;
        transform.position = m_Board.CellToWorld(cell);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _inputAction = InputSystem.actions.FindAction("Move");
    }

    // Update is called once per frame
    void Update()
    {
        Vector2Int newCellPosition = m_CellPosition;
        bool hasMoved = false;
        if (_inputAction.WasPressedThisFrame())
        {
            Vector2 movement = _inputAction.ReadValue<Vector2>();
            newCellPosition.x += (int)movement.x;
            newCellPosition.y += (int)movement.y;
            hasMoved = true;
        }

        if (hasMoved)
        { 
            CellData cellData = m_Board.CellData(newCellPosition);

            if (cellData != null && cellData.Passable)
            {
                MoveTo(newCellPosition);
            }
        }
    }
}
