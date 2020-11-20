using Game.Core;
using Lean.Touch;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Game.Mowers.Input
{
    internal class TouchDragHandler : MonoBehaviour, IMovementGestureHandler, IRequiresMowerPosition
    {
        [SerializeField] private ScreenToWorldPointConverter screenToWorldConverter;
        [SerializeField] private Positioner positioner;

        public event Action<GridVector> Move;

        private IMowerPosition _mowerPosition;

        public void Init(IMowerPosition mowerPosition)
        {
            _mowerPosition = mowerPosition;
        }

        public void OnFingerUpdate(LeanFinger finger)
        {

        }

        public void OnFingerUp(LeanFinger finger)
        {

        }
    }
}
