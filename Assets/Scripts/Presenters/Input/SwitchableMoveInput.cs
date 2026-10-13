using System;
using System.Collections.Generic;
using Game.Core;

namespace Lawnmower.Presenters
{
	/// <summary>
	/// One <see cref="IMoveInput"/> per <see cref="MovementScheme"/>, of which only the active one is forwarded,
	/// plus always-on sources (the keyboard, for editor testing). The scheme can be switched at any time.
	/// </summary>
	public sealed class SwitchableMoveInput : IMoveInput, IDisposable
	{
		public event Action<GridVector> MoveRequested;

		/// <summary>Raised with the new scheme when <see cref="Scheme"/> changes.</summary>
		public event Action<MovementScheme> SchemeChanged;

		private readonly IMoveInput _swipe;
		private readonly IMoveInput _arrows;
		private readonly IReadOnlyList<IMoveInput> _alwaysOn;

		private MovementScheme _scheme;

		public SwitchableMoveInput(IMoveInput swipe, IMoveInput arrows, MovementScheme scheme = MovementSchemeSetting.Default,
			params IMoveInput[] alwaysOn)
		{
			_swipe = swipe ?? throw new ArgumentNullException(nameof(swipe));
			_arrows = arrows ?? throw new ArgumentNullException(nameof(arrows));
			_alwaysOn = alwaysOn ?? Array.Empty<IMoveInput>();
			_scheme = scheme;

			_swipe.MoveRequested += OnSwipe;
			_arrows.MoveRequested += OnArrows;

			foreach (IMoveInput input in _alwaysOn)
			{
				input.MoveRequested += Forward;
			}
		}

		public MovementScheme Scheme
		{
			get => _scheme;
			set
			{
				if (_scheme == value)
				{
					return;
				}

				_scheme = value;
				SchemeChanged?.Invoke(value);
			}
		}

		public void Dispose()
		{
			_swipe.MoveRequested -= OnSwipe;
			_arrows.MoveRequested -= OnArrows;

			foreach (IMoveInput input in _alwaysOn)
			{
				input.MoveRequested -= Forward;
			}
		}

		private void OnSwipe(GridVector direction)
		{
			if (_scheme == MovementScheme.Swipe)
			{
				Forward(direction);
			}
		}

		private void OnArrows(GridVector direction)
		{
			if (_scheme == MovementScheme.Arrows)
			{
				Forward(direction);
			}
		}

		private void Forward(GridVector direction) => MoveRequested?.Invoke(direction);
	}
}
