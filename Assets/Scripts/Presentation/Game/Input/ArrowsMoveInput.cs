using System;
using Game.Core;
using Game.UI.Screens;
using Lawnmower.Presenters;
using UnityEngine;

namespace Game.Input
{
	/// <summary>
	/// Taps on the on-screen arrows. An arrow's screen direction is mapped onto the board like a swipe: projected onto
	/// the board plane from the screen centre, taking the dominant world axis, so "up" follows the camera.
	/// </summary>
	internal sealed class ArrowsMoveInput : IMoveInput, IDisposable
	{
		private const float ProbeLength = 100;

		public event Action<GridVector> MoveRequested;

		private readonly ArrowPadView _arrows;
		private readonly ScreenToWorldPointConverter _screenToBoard;

		public ArrowsMoveInput(ArrowPadView arrows, ScreenToWorldPointConverter screenToBoard)
		{
			_arrows = arrows;
			_screenToBoard = screenToBoard;

			if (_arrows != null)
			{
				_arrows.Pressed += OnArrowPressed;
			}
		}

		public void Dispose()
		{
			if (_arrows != null)
			{
				_arrows.Pressed -= OnArrowPressed;
			}
		}

		private void OnArrowPressed(Vector2 screenDirection)
		{
			GridVector direction = ToBoardDirection(screenDirection);
			if (direction.IsCardinalUnit)
			{
				MoveRequested?.Invoke(direction);
			}
		}

		private GridVector ToBoardDirection(Vector2 screenDirection)
		{
			if (_screenToBoard == null)
			{
				return SwipeRecognizer.ToCardinal(screenDirection.x, screenDirection.y);
			}

			Vector2 centre = new Vector2(Screen.width, Screen.height) * 0.5f;
			Vector3 start = _screenToBoard.GetWorldPoint(centre);
			Vector3 end = _screenToBoard.GetWorldPoint(centre + screenDirection * ProbeLength);
			Vector3 delta = end - start;
			return SwipeRecognizer.ToCardinal(delta.x, delta.z);
		}
	}
}
