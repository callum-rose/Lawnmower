using System;
using Game.Core;
using Lawnmower.Presenters;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game.Input
{
	/// <summary>Arrow keys and WASD (editor and desktop testing). Polled by <see cref="MoveInputController"/>.</summary>
	internal sealed class KeyboardMoveInput : IMoveInput
	{
		public event Action<GridVector> MoveRequested;

		public void Tick()
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard == null)
			{
				return;
			}

			if (Pressed(keyboard.rightArrowKey) || Pressed(keyboard.dKey))
			{
				MoveRequested?.Invoke(GridVector.Right);
			}
			else if (Pressed(keyboard.leftArrowKey) || Pressed(keyboard.aKey))
			{
				MoveRequested?.Invoke(GridVector.Left);
			}
			else if (Pressed(keyboard.upArrowKey) || Pressed(keyboard.wKey))
			{
				MoveRequested?.Invoke(GridVector.Up);
			}
			else if (Pressed(keyboard.downArrowKey) || Pressed(keyboard.sKey))
			{
				MoveRequested?.Invoke(GridVector.Down);
			}
		}

		private static bool Pressed(ButtonControl key) => key.wasPressedThisFrame;
	}
}
