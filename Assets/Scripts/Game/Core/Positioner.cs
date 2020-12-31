using UnityEngine;
using static Game.Levels.LevelDimensions;

namespace Game.Core
{
    public class Positioner : MonoBehaviour
    {
        [SerializeField] private Transform container;

        private Vector3 _initialPosition;

        private void Awake()
        {
            _initialPosition = transform.position;
        }

        public void Position(Transform transform, GridVector position)
        {
            if (transform.parent != container)
            {
               transform.SetParent(container);
            }

            transform.localPosition = GetLocalPosition(position);
        }

        public Vector3 GetLocalPosition(GridVector position)
        {
            return new Vector3((position.x + 0.5f) * TileSize, 0, (position.y + 0.5f) * TileSize);
        }

        public Vector3 GetWorldPosition(GridVector position)
        {
            return GetLocalPosition(position) + container.position;
        }

        public GridVector GetGridPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            int WorldAxisToGrid(float value) => Mathf.FloorToInt((value / TileSize));

            return new GridVector(WorldAxisToGrid(localPosition.x), WorldAxisToGrid(localPosition.z));
        }

        public void ZeroOffset()
        {
            transform.position = _initialPosition;
        }

        public void OffsetContainer(GridVector offset)
        {
            container.position += offset.ToXZ() * TileSize;
        }
    }
}
