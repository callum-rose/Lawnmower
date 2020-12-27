using Sirenix.OdinInspector;
using UnityEngine;
using static Game.Levels.LevelDimensions;

namespace Game.Levels.Editorr
{
    public class GizmoSelectedTileRenderer : MonoBehaviour
    {
        [SerializeField] private Transform tilesContainer;
        [SerializeField] private Color colour;

        [ShowInInspector]
        public int X { get; set; }
        [ShowInInspector]
        public int Y { get; set; }

        private void OnDrawGizmos()
        {
            Gizmos.color = colour;

            Vector3 bl = tilesContainer.position + new Vector3(X * TileSize, 0, Y * TileSize);
            Vector3 tl = bl + new Vector3(0, 0, 1) * TileSize;
            Vector3 tr = bl + new Vector3(1, 0, 1) * TileSize;
            Vector3 br = bl + new Vector3(1, 0, 0) * TileSize;

            Gizmos.DrawLine(bl, tl);
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
        }
    }
}