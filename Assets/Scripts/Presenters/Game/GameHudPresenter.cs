using System;

namespace Lawnmower.Presenters
{
	/// <summary>
	/// Binds the HUD and the Pause card to a <see cref="GamePresenter"/>: pause/resume, undo/redo availability, the
	/// level number and the Main Menu. Restart, Levels, Undo and Redo go through <see cref="IGameScreenView"/>.
	/// </summary>
	public sealed class GameHudPresenter : IDisposable
	{
		private readonly GamePresenter _game;
		private readonly IGameHudView _view;
		private readonly Progression _progression;
		private readonly IGameMenuNavigator _navigator;
		private bool _isStarted;

		/// <summary>The Pause card's SETTINGS button: the scene opens Settings on top of the card.</summary>
		public event Action SettingsRequested;

		public GameHudPresenter(GamePresenter game, IGameHudView view, Progression progression, IGameMenuNavigator navigator)
		{
			_game = game ?? throw new ArgumentNullException(nameof(game));
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_progression = progression ?? throw new ArgumentNullException(nameof(progression));
			_navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
		}

		public int LevelNumber => _progression.IndexOf(_game.LevelId) + 1;

		public void Start()
		{
			if (_isStarted)
			{
				return;
			}

			_isStarted = true;
			_game.StatsChanged += OnStatsChanged;
			_game.PausedChanged += OnPausedChanged;
			_view.PauseRequested += _game.Pause;
			_view.ResumeRequested += _game.Resume;
			_view.SettingsRequested += OnSettingsRequested;
			_view.MainMenuRequested += _navigator.LoadMainMenu;

			_view.ShowLevel(LevelNumber);
			_view.ShowHistory(_game.CanUndo, _game.CanRedo);
		}

		public void Dispose()
		{
			if (!_isStarted)
			{
				return;
			}

			_isStarted = false;
			_game.StatsChanged -= OnStatsChanged;
			_game.PausedChanged -= OnPausedChanged;
			_view.PauseRequested -= _game.Pause;
			_view.ResumeRequested -= _game.Resume;
			_view.SettingsRequested -= OnSettingsRequested;
			_view.MainMenuRequested -= _navigator.LoadMainMenu;
		}

		private void OnStatsChanged(AttemptStats _) => _view.ShowHistory(_game.CanUndo, _game.CanRedo);

		private void OnPausedChanged(bool paused)
		{
			if (paused)
			{
				_view.ShowPaused(new PauseInfo(LevelNumber, _progression.PerfectCount, _progression.LevelCount));
			}
			else
			{
				_view.HidePaused();
			}
		}

		private void OnSettingsRequested() => SettingsRequested?.Invoke();
	}
}
