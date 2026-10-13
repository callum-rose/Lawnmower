using System;

namespace Lawnmower.Presenters
{
	/// <summary>
	/// Counts the current attempt: moves (an undo takes one off; bumps never reach it), undos, and play time. The
	/// clock runs from <see cref="Start"/> and stops while paused or finished.
	/// </summary>
	public sealed class AttemptTracker
	{
		private readonly IClock _clock;

		private double _accumulated;
		private double? _runningSince;
		private bool _isStarted;

		public int Moves { get; private set; }
		public int Undos { get; private set; }
		public bool IsPaused { get; private set; }
		public bool IsFinished { get; private set; }

		public bool IsClockRunning => _runningSince.HasValue;

		/// <summary>Seconds of play so far, not counting pauses.</summary>
		public double Elapsed => _accumulated + (_runningSince.HasValue ? Math.Max(0, _clock.Now - _runningSince.Value) : 0);

		public AttemptStats Stats => new AttemptStats(Moves, Undos, Elapsed);

		public AttemptTracker(IClock clock)
		{
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		}

		/// <summary>Starts the clock (once; later calls do nothing).</summary>
		public void Start()
		{
			if (_isStarted)
			{
				return;
			}

			_isStarted = true;
			UpdateClock();
		}

		public void Pause() => SetPaused(true);
		public void Resume() => SetPaused(false);

		/// <summary>The level was completed (true) or that completion was undone (false). The clock stops while finished.</summary>
		public void SetFinished(bool isFinished)
		{
			IsFinished = isFinished;
			UpdateClock();
		}

		public void CountMove() => Moves++;

		/// <summary>An undo or a rewound move: one move off.</summary>
		public void CountMoveUndone() => Moves = Math.Max(0, Moves - 1);

		public void CountUndo() => Undos++;

		private void SetPaused(bool isPaused)
		{
			IsPaused = isPaused;
			UpdateClock();
		}

		private void UpdateClock()
		{
			bool shouldRun = _isStarted && !IsPaused && !IsFinished;

			if (shouldRun == _runningSince.HasValue)
			{
				return;
			}

			if (shouldRun)
			{
				_runningSince = _clock.Now;
			}
			else
			{
				_accumulated += Math.Max(0, _clock.Now - _runningSince.Value);
				_runningSince = null;
			}
		}
	}
}
