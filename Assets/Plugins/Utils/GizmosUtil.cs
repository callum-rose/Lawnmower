using UnityEngine;

namespace Utils
{
    public static class GizmosUtils
    {
        public static void DrawRect(Rect rect, float z = 0)
        {
            Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMin, z), new Vector3(rect.xMin, rect.yMax, z));
            Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMax, z), new Vector3(rect.xMax, rect.yMax, z));
            Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMax, z), new Vector3(rect.xMax, rect.yMin, z));
            Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMin, z), new Vector3(rect.xMin, rect.yMin, z));
        }
    }
}
