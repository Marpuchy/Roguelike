using UnityEngine;
using UnityEngine.Tilemaps;

public class WallObject : CellObject
{
    public Tile ObstacleTile;
    public Tile NearlyDestroyedTile;
    public int MaxHealth = 3;

    private int m_HealthPoint;
    private Tile m_OriginalTile;

    public override void Init(Vector2Int cell)
    {
        base.Init(cell);

        m_HealthPoint = MaxHealth;

        m_OriginalTile = GameManager.Instance.board.GetCelltile(cell);
        GameManager.Instance.board.SetCellTile(cell, ObstacleTile);
    }

    public override bool PlayerWantsToEnter()
    {
        m_HealthPoint--;

        if (m_HealthPoint > 0)
        {
            if (m_HealthPoint == 1)
            {
                GameManager.Instance.board.SetCellTile(MCell, NearlyDestroyedTile);
            }
            return false;
        }
        
        GameManager.Instance.board.SetCellTile(MCell, m_OriginalTile);
        Destroy(gameObject);
        return true;
    }
}
