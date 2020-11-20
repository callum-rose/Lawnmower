using Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
    public class LevelClickColliderSizer : MonoBehaviour
    {
        [SerializeField] private BoxCollider boxCollider;
        [SerializeField] private Positioner positioner;

        [Button(Expanded = true), BoxGroup("Debug")]
        public void SetSize(int levelWidth, int levelDepth)
        {
            GridVector bottomLeft = new GridVector(0, 0);
            GridVector topRight = new GridVector(levelWidth - 1, levelDepth - 1);

            Vector3 blVec = positioner.GetWorldPosition(bottomLeft) - new Vector3(0.5f, 1, 0.5f) * LevelDimensions.TileSize;
            Vector3 trVec = positioner.GetWorldPosition(topRight) + new Vector3(0.5f, 0, 0.5f) * LevelDimensions.TileSize;

            Bounds bounds = new Bounds(blVec, Vector3.zero);
            bounds.Encapsulate(trVec);

            boxCollider.center = bounds.center;
            boxCollider.size = bounds.size;
        }
    }
}
