using Sirenix.OdinInspector;
using UnityEngine;
using static Game.Levels.LevelDimensions;

namespace Game.LevelEditor
{
    public class GizmoGridRenderer : MonoBehaviour
    {
        [SerializeField] private Transform tilesContainer;
        [SerializeField] private Color colour;

        [ShowInInspector]
        public int Width { get; set; }
        [ShowInInspector]
        public int Depth { get; set; }

        private void OnDrawGizmos()
        {
            Gizmos.color = colour;

            Vector3 origin = tilesContainer.position;

            for (int y = 0; y <= Depth; y++)
            {
                Vector3 start = origin + new Vector3(0, 0, y * TileSize);
                Vector3 end = origin + new Vector3(Width * TileSize, 0, y * TileSize);
                Gizmos.DrawLine(start, end);
            }
            for (int x = 0; x <= Width; x++)
            {
                Vector3 start = origin + new Vector3(x * TileSize, 0, 0);
                Vector3 end = origin + new Vector3(x * TileSize, 0, Depth * TileSize);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}