using Game.Core;
using Lean.Touch;
using System;
using UnityEngine;

namespace Game.Mowers.Input
{
    internal class TouchDragHandler : MonoBehaviour, IMowerMovementGestureHandler, IRequiresMowerPosition
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
