using BalsamicBits.Extensions;
using Game.Levels;
using UnityEngine;
using Utils;
using static Game.Levels.LevelDimensions;

namespace Game.Core
{
    public class Positioner : MonoBehaviour
    {
        [SerializeField] private Transform container;


        private void Awake()
        {
            container.position = container.position.SetY(LevelBasePlaneY);
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
            return new Vector3((position.x + 0.5f) * TileSize, LevelBasePlaneY, (position.y + 0.5f) * TileSize);
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
        
        public Vector3 GetLevelWorldCenter(IReadOnlyLevelData level)
        {
            GridVector levelCenterMin = new GridVector(
                Mathf.FloorToInt((level.Width - 1) / 2f),
                Mathf.FloorToInt((level.Depth - 1) / 2f));
            GridVector levelCenterMax = new GridVector(
                Mathf.CeilToInt((level.Width - 1) / 2f),
                Mathf.CeilToInt((level.Depth - 1) / 2f));

            Vector3 worldCenterMin = GetWorldPosition(levelCenterMin);
            Vector3 worldCenterMax = GetWorldPosition(levelCenterMax);

            return (worldCenterMin + worldCenterMax) * 0.5f;
        }

        public float GetMaxDistanceFromLevelCenter(IReadOnlyLevelData level)
        {
            return GetMaxDistanceFromLevelCenter(level, GetLevelWorldCenter(level));
        }

        public float GetMaxDistanceFromLevelCenter(IReadOnlyLevelData level, Vector3 worldCenter)
        {
            float maxDistanceSqr = 0;
            Loops.TwoD(level.Width, level.Depth, (x, y) =>
            {
                Vector3 worldPosition = GetWorldPosition(new GridVector(x, y));
                float distanceSqr = Vector3.SqrMagnitude(worldPosition - worldCenter);

                if (distanceSqr > maxDistanceSqr)
                {
                    maxDistanceSqr = distanceSqr;
                }
            });

            return Mathf.Sqrt(maxDistanceSqr);
        }
    }
}
