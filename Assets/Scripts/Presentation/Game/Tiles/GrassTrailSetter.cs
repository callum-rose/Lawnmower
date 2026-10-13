using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.Tiles
{
    internal class GrassTrailSetter : MonoBehaviour
    {
        [SerializeField] private int wallThickness = 1;
        [SerializeField] private float heightDiff = 0.1f;

        private int squareSideLength;
        private Transform[] grassTransforms;

        public void SetTrail(GridVector directionIn, GridVector directionOut)
        {
            GetTransformsInRange(new RectInt());

        }

        private IEnumerable<Transform> GetTransformsInRange(RectInt range)
        {
            for (int y = range.yMin; y <= range.yMax; y++)
            {
                for (int x = range.xMin; x <= range.xMax; x++)
                {
                    GetTransform(x, y).localPosition = Vector3.down * heightDiff;
                }
            }

            return null;
        }

        private Transform[] GetSideTransforms(Side side)
        {
            List<Transform> transforms = new List<Transform>(squareSideLength - wallThickness * 2);
            switch (side)
            {
                case Side.North:
                case Side.South:
                {
                    int y = side == Side.South ? 0 : squareSideLength - 1;
                    for (int x = wallThickness; x < squareSideLength - wallThickness; x++)
                    {
                        transforms.Add(GetTransform(x, y));
                    }
                    break;
                }

                case Side.East:
                case Side.West:
                {
                    int x = side == Side.West ? 0 : squareSideLength - 1;
                    for (int y = wallThickness; y < squareSideLength - wallThickness; y++)
                    {
                        transforms.Add(GetTransform(x, y));
                    }
                    break;
                }
            }

            return transforms.ToArray();
        }

        private Transform GetTransform(int x, int y)
        {
            return grassTransforms[x + squareSideLength * y];
        }

        private void GetGrassTransforms()
        {
            squareSideLength = (int)Mathf.Sqrt(transform.childCount);
            grassTransforms = (transform as IEnumerable<Transform>)
                .OrderBy(t => t.localPosition.z)
                .ThenBy(t => t.localPosition.x)
                .ToArray();
        }

        private enum Side
        {
            North, East, South, West
        }
    }
}
