using System;
using DefaultNamespace;
using DefaultNamespace.Interface;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    public int MoveSpeed = 2;
    private BoardManager m_Board;

    private Vector2Int m_CellPosition;
    public Vector2Int Cell;

    private InputAction m_InputAction;
    private InputAction m_RestartAction;

    private bool m_IsGameOver;
    private Animator m_Animator;
    private bool m_IsMoving;
    private Vector3 m_MoveTarget;
    
    public UnityEvent PlayerCheck;
    
    public void Awake()
    {
        m_Animator = GetComponent<Animator>();
       
    }

    public void Init()
    {
        m_IsGameOver = false;
    }
    public void Spawn(BoardManager boardManager, Vector2Int cell)
    {
        m_Board = boardManager;
        MoveTo(cell, true);
        GameManager.Instance.TurnManager.OnTick += OnTurnHappen;
        PlayerCheck.Invoke();
    }

    public void MoveTo(Vector2Int cell, bool immediate)
    {
        m_CellPosition = cell;
        Cell = m_CellPosition;
        if (immediate)
        {
            m_IsMoving = false;
            transform.position = m_Board.CellToWorld(m_CellPosition);
        }
        else
        {
            m_IsMoving = true;
            m_MoveTarget = m_Board.CellToWorld(m_CellPosition);
        }
        m_Animator.SetBool("Moving", m_IsMoving);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_InputAction = InputSystem.actions.FindAction("Move");
        m_RestartAction = InputSystem.actions.FindAction("Restart");
    }

    // Update is called once per frame
    void Update()
    {
        if (m_IsGameOver)
        {
            if (m_RestartAction.WasPressedThisFrame())
            {
                GameManager.Instance.StartNewGame();
            }

            return;
        }

        
        Vector2Int newCellPosition = m_CellPosition;
        bool hasMoved = false;
        if (m_InputAction.WasPressedThisFrame())
        {
            Vector2 movement = m_InputAction.ReadValue<Vector2>();
            newCellPosition.x += (int)movement.x;
            newCellPosition.y += (int)movement.y;
            hasMoved = true;
        }

        if (hasMoved)
        { 
            CellData cellData = m_Board.CellData(newCellPosition);

            if (cellData != null && cellData.Passable)
            {
                GameManager.Instance.TurnManager.Tick();
                
                if (cellData.ContainedObject == null)
                {
                    MoveTo(newCellPosition, false);
                }
                else if (cellData.ContainedObject.PlayerWantsToEnter())
                {
                    MoveTo(newCellPosition, false);
                    cellData.ContainedObject.PlayerEntered();
                }
            }
        }

        if (m_IsMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, m_MoveTarget, MoveSpeed * Time.deltaTime);
          
            if (transform.position == m_MoveTarget)
            {
                m_IsMoving = false;
                m_Animator.SetBool("Moving", false);
                var cellData = m_Board.CellData(m_CellPosition);
                if(cellData.ContainedObject != null)
                    cellData.ContainedObject.PlayerEntered();
            }

            return;
        }
    }

    public void GameOver()
    {
        m_IsGameOver = true;
    }

    public void OnTurnHappen()
    {
        PlayerCheck?.Invoke();
    }
}
