using System;
using Game.Core;

namespace Lawnmower.Presenters
{
	/// <summary>
	/// The tutorial: first "move" (a swipe or an arrow tap, following <see cref="MovementSchemeSetting"/>), which
	/// ends on the first move, then "mow the rest", which the view dismisses. Finishing marks it seen.
	/// </summary>
	public sealed class TutorialPresenter : IDisposable
	{
		private readonly ITutorialView _view;
		private readonly IMoveInput _input;
		private readonly Action _markSeen;

		public TutorialStep? Step { get; private set; }

		public bool IsRunning => Step.HasValue;

		public event Action Finished;

		public TutorialPresenter(ITutorialView view, IMoveInput input, Action markSeen)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_input = input ?? throw new ArgumentNullException(nameof(input));
			_markSeen = markSeen;
		}

		public void Start()
		{
			if (IsRunning)
			{
				return;
			}

			_input.MoveRequested += OnMoveRequested;
			_view.Dismissed += OnDismissed;
			MovementSchemeSetting.Changed += OnSchemeChanged;
			Show(MoveStep(MovementSchemeSetting.Current));
		}

		/// <summary>Stops without marking the tutorial seen (the level was left part-way).</summary>
		public void Dispose()
		{
			if (!IsRunning)
			{
				return;
			}

			Detach();
			_view.Hide();
		}

		private static TutorialStep MoveStep(MovementScheme scheme) =>
			scheme == MovementScheme.Arrows ? TutorialStep.TapArrows : TutorialStep.Swipe;

		private void Show(TutorialStep step)
		{
			Step = step;
			_view.ShowStep(step);
		}

		private void OnMoveRequested(GridVector _)
		{
			if (Step is TutorialStep.Swipe or TutorialStep.TapArrows)
			{
				Show(TutorialStep.MowTheRest);
			}
		}

		private void OnSchemeChanged(MovementScheme scheme)
		{
			if (Step is TutorialStep.Swipe or TutorialStep.TapArrows)
			{
				Show(MoveStep(scheme));
			}
		}

		private void OnDismissed()
		{
			if (Step != TutorialStep.MowTheRest)
			{
				return;
			}

			Detach();
			_view.Hide();
			_markSeen?.Invoke();
			Finished?.Invoke();
		}

		private void Detach()
		{
			Step = null;
			_input.MoveRequested -= OnMoveRequested;
			_view.Dismissed -= OnDismissed;
			MovementSchemeSetting.Changed -= OnSchemeChanged;
		}
	}
}
