using System;
using Game.Core;
using Game.UI;
using Lawnmower.Presenters;

namespace Game.Input
{
	/// <summary>Taps on the on-screen arrows.</summary>
	internal sealed class ArrowsMoveInput : IMoveInput, IDisposable
	{
		public event Action<GridVector> MoveRequested;

		private readonly UiArrowsManager _arrows;

		public ArrowsMoveInput(UiArrowsManager arrows)
		{
			_arrows = arrows;

			if (_arrows != null)
			{
				_arrows.ArrowPressed += OnArrowPressed;
			}
		}

		public void Dispose()
		{
			if (_arrows != null)
			{
				_arrows.ArrowPressed -= OnArrowPressed;
			}
		}

		private void OnArrowPressed(GridVector direction) => MoveRequested?.Invoke(direction);
	}
}
