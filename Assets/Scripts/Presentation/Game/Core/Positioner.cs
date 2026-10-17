using System.Collections.Generic;
using BalsamicBits.Extensions;
using Game.Levels;
using UnityEngine;
using Utils;
using static Game.Levels.LevelDimensions;
using Lawnmower.Model;

namespace Game.Core
{
    public class Positioner : MonoBehaviour
    {
        [SerializeField] private Transform container;

        // Tiles placed with a surface the mower stands on; a destroyed tile reads as null and falls back to the ground
        private readonly Dictionary<GridVector, Component> _standSurfaces = new Dictionary<GridVector, Component>();

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

            if (transform.TryGetComponent(out IStandSurface surface))
            {
                _standSurfaces[position] = (Component)surface;
            }
            else
            {
                _standSurfaces.Remove(position);
            }
        }

        public Vector3 GetLocalPosition(GridVector position)
        {
            return new Vector3((position.x + 0.5f) * TileSize, LevelBasePlaneY, (position.y + 0.5f) * TileSize);
        }

        public Vector3 GetWorldPosition(GridVector position)
        {
            return GetLocalPosition(position) + container.position;
        }

        /// <summary>Where the mower stands on a tile: its world position, raised onto any <see cref="IStandSurface"/>.</summary>
        public Vector3 GetStandWorldPosition(GridVector position)
        {
            Vector3 worldPosition = GetWorldPosition(position);

            if (_standSurfaces.TryGetValue(position, out Component surface) && surface)
            {
                worldPosition.y += ((IStandSurface)surface).StandHeight;
            }

            return worldPosition;
        }

        public GridVector GetGridPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            int WorldAxisToGrid(float value) => Mathf.FloorToInt((value / TileSize));

            return new GridVector(WorldAxisToGrid(localPosition.x), WorldAxisToGrid(localPosition.z));
        }
        
        public Vector3 GetLevelWorldCenter(Level level)
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

        public float GetMaxDistanceFromLevelCenter(Level level)
        {
            return GetMaxDistanceFromLevelCenter(level, GetLevelWorldCenter(level));
        }

        public float GetMaxDistanceFromLevelCenter(Level level, Vector3 worldCenter)
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
