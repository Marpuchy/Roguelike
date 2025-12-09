using UnityEngine;
using UnityEngine.Tilemaps;

namespace DefaultNamespace
{
    public class ExitCellObject : CellObject
    {
        public Tile EndTile;

        public override void Init(Vector2Int coord)
        {
            base.Init(coord);
            GameManager.Instance.board.SetCellTile(coord, EndTile);
        }

        public override void PlayerEntered()
        {
            GameManager.Instance.NewLevel();
        }
    }
}