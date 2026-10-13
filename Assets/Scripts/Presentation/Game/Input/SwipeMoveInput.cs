using System;
using System.Collections.Generic;
using Game.Core;
using Lawnmower.Presenters;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Input
{
	/// <summary>
	/// Swipes on the board: the primary touch on a device, the mouse in the editor. A swipe is mapped onto the board
	/// by projecting its start and end onto the board plane and taking the dominant world axis, so "up the screen"
	/// follows the camera. Touches that start on UI are ignored. Polled by <see cref="MoveInputController"/>.
	/// </summary>
	internal sealed class SwipeMoveInput : IMoveInput
	{
		/// <summary>Assumed when the platform doesn't report a DPI (the editor, some desktops).</summary>
		private const float FallbackDpi = 160;

		public event Action<GridVector> MoveRequested;

		private readonly ScreenToWorldPointConverter _screenToBoard;
		private readonly SwipeRecognizer _recognizer;
		private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

		private bool _wasPressed;

		public SwipeMoveInput(ScreenToWorldPointConverter screenToBoard, float minDistanceInches, float minStraightness)
		{
			_screenToBoard = screenToBoard;
			float dpi = Screen.dpi > 0 ? Screen.dpi : FallbackDpi;
			_recognizer = new SwipeRecognizer(minDistanceInches * dpi, minStraightness);
		}

		public void Reset()
		{
			_recognizer.End();
			_wasPressed = false;
		}

		public void Tick()
		{
			Pointer pointer = Pointer.current;
			bool isPressed = pointer != null && pointer.press.isPressed;

			if (!isPressed)
			{
				if (_wasPressed)
				{
					_recognizer.End();
				}

				_wasPressed = false;
				return;
			}

			Vector2 position = pointer.position.ReadValue();

			if (!_wasPressed)
			{
				_wasPressed = true;

				if (!IsOverUi(position))
				{
					_recognizer.Begin(position.x, position.y);
				}

				return;
			}

			if (_recognizer.Move(position.x, position.y, out Swipe swipe))
			{
				GridVector direction = ToBoardDirection(swipe);
				if (direction.IsCardinalUnit)
				{
					MoveRequested?.Invoke(direction);
				}
			}
		}

		private GridVector ToBoardDirection(Swipe swipe)
		{
			Vector3 start = _screenToBoard.GetWorldPoint(new Vector2(swipe.StartX, swipe.StartY));
			Vector3 end = _screenToBoard.GetWorldPoint(new Vector2(swipe.EndX, swipe.EndY));
			Vector3 delta = end - start;
			return SwipeRecognizer.ToCardinal(delta.x, delta.z);
		}

		/// <summary>Only canvas UI counts: the board itself is hit by the camera's physics raycaster.</summary>
		private bool IsOverUi(Vector2 screenPosition)
		{
			EventSystem eventSystem = EventSystem.current;
			if (eventSystem == null)
			{
				return false;
			}

			_raycastResults.Clear();
			eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screenPosition }, _raycastResults);

			foreach (RaycastResult result in _raycastResults)
			{
				if (result.module is GraphicRaycaster)
				{
					return true;
				}
			}

			return false;
		}
	}
}
