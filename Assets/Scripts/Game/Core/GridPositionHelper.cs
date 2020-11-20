using UnityEngine;

namespace Game.Core
{
    internal static class GridPositionHelper
    {
        public static Vector3 MakeXZ(GridVector position)
        {
            return new Vector3(position.x, 0, position.y);
        }
    }
}
