using Game.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Touch;
using System.Linq;

namespace Game.Mowers.Input
{
    [DefaultExecutionOrder(-99)]
    class TouchMowerControls : MonoBehaviour, IMowerControls
    {
        [SerializeField] private IMovementGestureHandlerContainer[] _gestureHandlerContainers;

        public event Action<GridVector> Moved;

        private void OnEnable()
        {
            foreach (var gh in _gestureHandlerContainers.Select(c => c.Result))
            {
                gh.Move += OnTouchGestureMoved;
            }
        }

        private void OnDisable()
        {
            foreach (var gh in _gestureHandlerContainers.Select(c => c.Result))
            {
                gh.Move -= OnTouchGestureMoved;
            }
        }

        private void OnTouchGestureMoved(GridVector direction)
        {
            Moved.Invoke(direction);
        }
    }
}