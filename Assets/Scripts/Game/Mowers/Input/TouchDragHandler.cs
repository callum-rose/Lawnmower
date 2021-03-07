using Game.Core;
using Lean.Touch;
using System;
using UnityEngine;

namespace Game.Mowers.Input
{
	internal class TouchDragHandler : MonoBehaviour, IMowerMovementGestureHandler, INeedMowerPosition
	{
		[SerializeField] private ScreenToWorldPointConverter screenToWorldConverter;
		[SerializeField] private Positioner positioner;

		public event Action<GridVector> Move;

		private IMowerPosition _mowerPosition;

		void INeedMowerPosition.Set(IMowerPosition mowerPosition)
		{
			_mowerPosition = mowerPosition;
		}

		void INeedMowerPosition.Clear()
		{
			_mowerPosition = null;
		}

		public void OnFingerUpdate(LeanFinger finger)
		{
		}

		public void OnFingerUp(LeanFinger finger)
		{
		}
	}
}