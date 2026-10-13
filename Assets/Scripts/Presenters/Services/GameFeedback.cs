using System;
using Game.UndoSystem;
using Lawnmower.Model;

namespace Lawnmower.Presenters
{
	public enum SoundEffect
	{
		Move,
		Bump,
		Click,
		Win,
		Fail
	}

	/// <summary>Plays one-shot sound effects. The Unity implementation routes them through the mixer's SFX group.</summary>
	public interface ISoundEffects
	{
		void Play(SoundEffect effect);
	}

	/// <summary>Taptic feedback. The Unity implementation calls the iOS plugin and does nothing in the Editor.</summary>
	public interface IHaptics
	{
		void LightTap();
		void Success();
		void Error();
	}

	/// <summary>
	/// Sounds and haptics for one play session, driven by the simulation's events: a move plays the move sound and a
	/// light tap (an undo only the sound), a bump the bump sound, a win the win sound and success haptic, a fail the
	/// fail sound and error haptic. Haptics follow <see cref="GameSettings.Haptics"/>. It only listens: it never calls
	/// Move or Undo.
	/// </summary>
	public sealed class GameFeedbackPresenter : IDisposable
	{
		private readonly LevelSimulation _simulation;
		private readonly ISoundEffects _sounds;
		private readonly IHaptics _haptics;
		private readonly GameSettings _settings;

		private (LevelStatus to, bool inverted)? _pendingStatus;
		private bool _isDisposed;

		public GameFeedbackPresenter(LevelSimulation simulation, ISoundEffects sounds, IHaptics haptics, GameSettings settings)
		{
			_simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
			_sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
			_haptics = haptics ?? throw new ArgumentNullException(nameof(haptics));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));

			_simulation.Moved += OnMoved;
			_simulation.Bumped += OnBumped;
			_simulation.StatusChanged += OnStatusChanged;
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
			_simulation.Moved -= OnMoved;
			_simulation.Bumped -= OnBumped;
			_simulation.StatusChanged -= OnStatusChanged;
		}

		private void OnStatusChanged(LevelStatus from, LevelStatus to, Xor inverted)
		{
			// Raised before Moved inside the same step; played after the move's feedback.
			_pendingStatus = (to, inverted);
		}

		private void OnMoved(MoveResult move, Xor inverted)
		{
			_sounds.Play(SoundEffect.Move);

			if (!inverted && _settings.Haptics)
			{
				_haptics.LightTap();
			}

			if (!_pendingStatus.HasValue)
			{
				return;
			}

			(LevelStatus to, bool statusInverted) = _pendingStatus.Value;
			_pendingStatus = null;

			if (statusInverted)
			{
				return;
			}

			if (to == LevelStatus.Completed)
			{
				_sounds.Play(SoundEffect.Win);

				if (_settings.Haptics)
				{
					_haptics.Success();
				}
			}
			else if (to == LevelStatus.Failed)
			{
				_sounds.Play(SoundEffect.Fail);

				if (_settings.Haptics)
				{
					_haptics.Error();
				}
			}
		}

		private void OnBumped(MoveResult bump)
		{
			_sounds.Play(SoundEffect.Bump);
		}
	}
}
