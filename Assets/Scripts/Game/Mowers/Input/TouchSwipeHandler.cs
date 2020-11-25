using Game.Core;
using Lean.Touch;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Game.Mowers.Input
{
    internal class TouchSwipeHandler : MonoBehaviour, IMowerMovementGestureHandler
    {
        [SerializeField] private ScreenToWorldPointConverter screenToWorldConverter;

        public event Action<GridVector> Move;

        public void OnFingerMove(LeanFinger finger)
        {
            Vector2 start = finger.StartScreenPosition;
            Vector2 end = finger.ScreenPosition;

            Vector3 startWorld = screenToWorldConverter.GetWorldPoint(start);
            Vector3 endWorld = screenToWorldConverter.GetWorldPoint(end);

            Vector3 worldDelta = endWorld - startWorld;
            GridVector worldVec;
            if (Mathf.Abs(worldDelta.x) > Mathf.Abs(worldDelta.z))
            {
                worldVec = new GridVector(Mathf.RoundToInt(Mathf.Sign(worldDelta.x)), 0);
            }
            else
            {
                worldVec = new GridVector(0, Mathf.RoundToInt(Mathf.Sign(worldDelta.z)));
            }

            Move?.Invoke(worldVec);
        }
    }
}
