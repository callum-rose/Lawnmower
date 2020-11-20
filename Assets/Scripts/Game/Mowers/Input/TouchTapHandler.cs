using BalsamicBits.Extensions;
using Game.Core;
using Game.Levels;
using Lean.Touch;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Game.Mowers.Input
{
    internal class TouchTapHandler : MonoBehaviour, IMovementGestureHandler, IRequiresMowerPosition
    {
        [SerializeField] private ScreenToWorldPointConverter screenToWorldConverter;
        [SerializeField] private Positioner positioner;
        [SerializeField, Range(0, 45)] private float toleranceAngle = 45;
        [SerializeField, Min(0)] private float maxDistanceFromMower;

#if UNITY_EDITOR
        [ShowInInspector, DisableIf(nameof(DisableFakeMowerPos))] private GridVector fakeMowerPos;
        private bool DisableFakeMowerPos => _mowerPosition != null;
#endif

        public event Action<GridVector> Move;

        private IMowerPosition _mowerPosition;

        #region Unity

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;

            Vector3 worldMowerPos = positioner.GetWorldPosition(_mowerPosition == null ? fakeMowerPos : _mowerPosition.MowerPosition);

            GridVector[] cardinalVectors = new GridVector[] { GridVector.Right, GridVector.Down, GridVector.Left, GridVector.Up };

            foreach (var cv in cardinalVectors)
            {
                GetConeDirections(cv, out Vector3 cone0LeftDirection, out Vector3 cone0RightDirection);
                GetConeLineOrigins(worldMowerPos, cv, out Vector3 coneLeftOrigin, out Vector3 coneRightOrigin);

                Gizmos.DrawLine(coneLeftOrigin, coneLeftOrigin + cone0LeftDirection * maxDistanceFromMower);
                Gizmos.DrawLine(coneRightOrigin, coneRightOrigin + cone0RightDirection * maxDistanceFromMower);
            }

            Gizmos.DrawWireCube(worldMowerPos, new Vector3(LevelDimensions.TileSize, 0, LevelDimensions.TileSize));
        }

        private static void GetConeLineOrigins(Vector3 worldMowerPos, GridVector cv, out Vector3 coneLeftOrigin, out Vector3 coneRightOrigin)
        {
            coneLeftOrigin = worldMowerPos + cv.ToXZ().Rotate(new Vector3(0, -45, 0)) * Mathf.Sqrt(2) * 0.5f;
            coneRightOrigin = worldMowerPos + cv.ToXZ().Rotate(new Vector3(0, 45, 0)) * Mathf.Sqrt(2) * 0.5f;
        }

        private void GetConeDirections(GridVector cardinalVector, out Vector3 cone0Left, out Vector3 cone0Right)
        {
            cone0Left = cardinalVector.ToXZ().Rotate(new Vector3(0, -toleranceAngle, 0)).normalized;
            cone0Right = cardinalVector.ToXZ().Rotate(new Vector3(0, toleranceAngle, 0)).normalized;
        }
#endif

        #endregion

        #region API

        public void Init(IMowerPosition mowerPosition)
        {
            _mowerPosition = mowerPosition;
        }

        public void OnFingerMove(LeanFinger finger)
        {
            Vector3 tapWorldPos = screenToWorldConverter.GetWorldPoint(finger.ScreenPosition);
            GridVector gridPos = positioner.GetGridPosition(tapWorldPos);

            GridVector mowerPos = _mowerPosition.MowerPosition;

            if (gridPos == mowerPos)
            {
                return;
            }

            if (IsTapTooFarAway(tapWorldPos))
            {
                return;
            }

            bool test = IsWorldPositionWithinToleranceAngle(tapWorldPos, out var direction);
            if (test)
            {
                Move?.Invoke(direction);
            }
        }

        #endregion

        #region Methods

        private bool IsTapTooFarAway(Vector3 tapWorldPos)
        {
            return Vector3.SqrMagnitude(tapWorldPos - positioner.GetWorldPosition(_mowerPosition.MowerPosition)) > maxDistanceFromMower * maxDistanceFromMower;
        }

        private bool IsWorldPositionWithinToleranceAngle(Vector3 position, out GridVector closestCardinalDirection)
        {
            Vector3 mowerWorldPos = positioner.GetWorldPosition(_mowerPosition.MowerPosition);
            Vector3 displacementFromMower = position - mowerWorldPos;

            GridVector gridDisplacement = positioner.GetGridPosition(displacementFromMower);

            if (gridDisplacement == GridVector.Zero)
            {
                closestCardinalDirection = GridVector.Zero;
                return false;
            }

            closestCardinalDirection = GetClosestCardinalDirection(displacementFromMower);

            Vector3 displacementFromConeOriginToPosition = GetDisplacementFromConeOriginToPosition(position, closestCardinalDirection, mowerWorldPos);

            float angle = Vector3.Angle(closestCardinalDirection.ToXZ(), displacementFromConeOriginToPosition);
            return angle <= toleranceAngle;
        }

        private Vector3 GetDisplacementFromConeOriginToPosition(Vector3 position, GridVector closestCardinalDirection, Vector3 mowerWorldPos)
        {
            float positionExtension = LevelDimensions.TileSize * ((1f / (2 * Mathf.Tan(toleranceAngle * Mathf.Deg2Rad))) - 0.5f);
            Vector3 toleranceConeOrigin = mowerWorldPos - closestCardinalDirection.ToXZ() * positionExtension;
            Vector3 displacementFromConeOriginToPosition = position - toleranceConeOrigin;
            return displacementFromConeOriginToPosition;
        }

        private static GridVector GetClosestCardinalDirection(Vector3 displacement)
        {
            if (Mathf.Abs(displacement.x) > Mathf.Abs(displacement.z))
            {
                return displacement.x > 0 ? GridVector.Right : GridVector.Left;
            }
            else
            {
                return displacement.z > 0 ? GridVector.Up : GridVector.Down;
            }
        }

        #endregion
    }
}