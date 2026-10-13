using System;
using System.Collections.Generic;
using Game.Core;
using Game.UndoSystem;
using Lawnmower.Model;

namespace Lawnmower.Presenters
{
	/// <summary>
	/// One play session: owns the <see cref="LevelSimulation"/>, turns input and screen requests into
	/// simulation calls, and turns simulation events into view calls. Holds no Unity state.
	/// </summary>
	/// <remarks>
	/// The simulation throws if Move/Undo/Redo is re-entered from one of its own events, so a command
	/// that arrives while a simulation call is running (a view reacting synchronously to an event) is
	/// queued and run straight after it.
	/// </remarks>
	public sealed class GamePresenter : IDisposable
	{
		private readonly GameSessionSetup _setup;
		private readonly IBoardView _board;
		private readonly IMowerView _mower;
		private readonly IGameScreenView _screen;
		private readonly IGameNavigator _navigator;
		private readonly ILevelProgress _progress;
		private readonly IClock _clock;
		private readonly IUndoSystem _undoSystem;

		private readonly List<IMoveInput> _inputs = new List<IMoveInput>();
		private readonly Queue<Action> _pendingCommands = new Queue<Action>();

		private bool _isRunningCommand;
		private bool _isDisposed;
		private int _moves;
		private int _undos;
		private double? _startTime;
		private (LevelStatus from, LevelStatus to, Xor inverted)? _pendingStatusChange;

		/// <summary>Null until <see cref="Start"/>.</summary>
		public LevelSimulation Simulation { get; private set; }

		/// <summary>False until <see cref="BeginPlay"/> (the level intro) and after <see cref="Dispose"/>.</summary>
		public bool IsAcceptingInput { get; private set; }

		public int Par => _setup.Par;

		public AttemptStats Stats =>
			new AttemptStats(_moves, _undos, _startTime.HasValue ? _clock.Now - _startTime.Value : 0);

		public GamePresenter(GameSessionSetup setup, IBoardView board, IMowerView mower, IGameScreenView screen,
			IGameNavigator navigator, ILevelProgress progress, IClock clock, IUndoSystem undoSystem = null)
		{
			_setup = setup ?? throw new ArgumentNullException(nameof(setup));
			_board = board ?? throw new ArgumentNullException(nameof(board));
			_mower = mower ?? throw new ArgumentNullException(nameof(mower));
			_screen = screen ?? throw new ArgumentNullException(nameof(screen));
			_navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
			_progress = progress ?? throw new ArgumentNullException(nameof(progress));
			_clock = clock ?? throw new ArgumentNullException(nameof(clock));
			_undoSystem = undoSystem ?? new UndoSystem();
		}

		#region API

		/// <summary>Builds the simulation and shows the board and mower. Input stays closed until <see cref="BeginPlay"/>.</summary>
		public void Start()
		{
			ThrowIfDisposed();

			if (Simulation != null)
			{
				throw new InvalidOperationException("Already started");
			}

			Simulation = new LevelSimulation(Level.FromDefinition(_setup.Definition), _undoSystem);
			Simulation.Moved += OnMoved;
			Simulation.Bumped += OnBumped;
			Simulation.StatusChanged += OnStatusChanged;

			_screen.UndoRequested += Undo;
			_screen.RedoRequested += Redo;
			_screen.RestartRequested += OnRestartRequested;
			_screen.NextLevelRequested += OnNextLevelRequested;
			_screen.LevelSelectRequested += OnLevelSelectRequested;

			_board.Show(Simulation.Level);
			_mower.Show(Simulation.Level.StartPosition);
			_screen.ShowProgress(_moves, Par);

			if (Simulation.Status == LevelStatus.Failed)
			{
				_screen.ShowFailed();
			}
		}

		/// <summary>The intro has finished: accept moves and start the attempt clock.</summary>
		public void BeginPlay()
		{
			ThrowIfDisposed();

			if (Simulation == null)
			{
				throw new InvalidOperationException("Start must be called before BeginPlay");
			}

			IsAcceptingInput = true;
			_startTime ??= _clock.Now;
		}

		public void AttachInput(IMoveInput input)
		{
			if (input == null || _inputs.Contains(input))
			{
				return;
			}

			_inputs.Add(input);
			input.MoveRequested += RequestMove;
		}

		public void DetachInput(IMoveInput input)
		{
			if (input != null && _inputs.Remove(input))
			{
				input.MoveRequested -= RequestMove;
			}
		}

		/// <summary>
		/// Ignored before <see cref="BeginPlay"/> and while the level is Failed: the model would accept the
		/// move, but the Level Failed card covers the board until the player undoes or restarts.
		/// </summary>
		public void RequestMove(GridVector direction)
		{
			if (!IsAcceptingInput || !direction.IsCardinalUnit || Simulation.Status == LevelStatus.Failed)
			{
				return;
			}

			Run(() => Simulation.Move(direction));
		}

		/// <summary>Undo one move (also the Failed card's "Undo Last Move").</summary>
		public void Undo()
		{
			if (!IsAcceptingInput)
			{
				return;
			}

			Run(() =>
			{
				if (_undoSystem.Undo())
				{
					_undos++;
				}
			});
		}

		/// <summary>Ignored while the level is Failed, like moves.</summary>
		public void Redo()
		{
			if (!IsAcceptingInput || Simulation.Status == LevelStatus.Failed)
			{
				return;
			}

			Run(() => _undoSystem.Redo());
		}

		/// <summary>
		/// Detaches from the simulation, the inputs and the screen. It doesn't clear the board or the mower:
		/// the owner does that when it reuses the views (a scene unload destroys them anyway).
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
			IsAcceptingInput = false;
			_pendingCommands.Clear();

			foreach (IMoveInput input in _inputs)
			{
				input.MoveRequested -= RequestMove;
			}

			_inputs.Clear();

			if (Simulation == null)
			{
				return;
			}

			Simulation.Moved -= OnMoved;
			Simulation.Bumped -= OnBumped;
			Simulation.StatusChanged -= OnStatusChanged;

			_screen.UndoRequested -= Undo;
			_screen.RedoRequested -= Redo;
			_screen.RestartRequested -= OnRestartRequested;
			_screen.NextLevelRequested -= OnNextLevelRequested;
			_screen.LevelSelectRequested -= OnLevelSelectRequested;

			_undoSystem.Reset();
		}

		#endregion

		#region Simulation events

		private void OnStatusChanged(LevelStatus from, LevelStatus to, Xor inverted)
		{
			// Raised before Moved inside the same undo step; handled in OnMoved so the move count is final.
			_pendingStatusChange = (from, to, inverted);
		}

		private void OnMoved(MoveResult move, Xor inverted)
		{
			if (inverted)
			{
				_moves--;
				_mower.RewindMove(move);
			}
			else
			{
				_moves++;
				_mower.PlayMove(move);
			}

			_screen.ShowProgress(_moves, Par);

			if (_pendingStatusChange.HasValue)
			{
				(LevelStatus from, LevelStatus to, Xor statusInverted) = _pendingStatusChange.Value;
				_pendingStatusChange = null;
				ShowStatusChange(from, to, statusInverted);
			}
		}

		private void OnBumped(MoveResult bump)
		{
			_mower.PlayBump(bump);
		}

		private void ShowStatusChange(LevelStatus from, LevelStatus to, Xor inverted)
		{
			if (from == LevelStatus.Completed && inverted)
			{
				_screen.HideCompleted();
			}

			if (from == LevelStatus.Failed && inverted)
			{
				_screen.HideFailed();
			}

			if (to == LevelStatus.Completed && !inverted)
			{
				AttemptStats stats = Stats;
				_progress.RecordCompleted(_setup.Definition.Id, stats, Par);
				_screen.ShowCompleted(new LevelCompletedInfo(stats.Moves, Par, _navigator.HasNextLevel));
			}

			if (to == LevelStatus.Failed && !inverted)
			{
				_screen.ShowFailed();
			}
		}

		#endregion

		#region Screen requests

		private void OnRestartRequested() => _navigator.RestartLevel();

		private void OnNextLevelRequested()
		{
			if (_navigator.HasNextLevel)
			{
				_navigator.LoadNextLevel();
			}
			else
			{
				_navigator.LoadLevelSelect();
			}
		}

		private void OnLevelSelectRequested() => _navigator.LoadLevelSelect();

		#endregion

		#region Methods

		private void Run(Action command)
		{
			if (_isRunningCommand)
			{
				_pendingCommands.Enqueue(command);
				return;
			}

			_isRunningCommand = true;

			try
			{
				command();

				while (_pendingCommands.Count > 0 && !_isDisposed)
				{
					_pendingCommands.Dequeue()();
				}
			}
			finally
			{
				_pendingCommands.Clear();
				_isRunningCommand = false;
			}
		}

		private void ThrowIfDisposed()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(GamePresenter));
			}
		}

		#endregion
	}
}
