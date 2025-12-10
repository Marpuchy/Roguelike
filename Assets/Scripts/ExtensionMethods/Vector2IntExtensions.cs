using UnityEngine;

namespace DefaultNamespace.ExtensionMethods
{
    public static class Vector2IntExtensions
    {
        public static bool IsAdjacentTo(this Vector2Int from, Vector2Int to)
        {
            int xDist = from.x - to.x;
            int yDist = from.y - to.y;

            int absXDist = Mathf.Abs(xDist);
            int absYDist = Mathf.Abs(yDist);

            return (xDist == 0 && absYDist == 1) || (yDist == 0 && absXDist == 1) ;
        }
    }
}