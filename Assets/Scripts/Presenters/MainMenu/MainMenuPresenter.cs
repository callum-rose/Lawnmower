using System;

namespace Lawnmower.Presenters
{
	public interface IMainMenuView
	{
		event Action PlayClicked;
		event Action LevelsClicked;
		event Action SettingsClicked;
		event Action HowToPlayClicked;

		/// <summary>The hidden ~2 s hold on the title.</summary>
		event Action TitleLongPressed;

		/// <summary>Labels the Play button with the level it starts; null when every level is done ("PLAY").</summary>
		void ShowPlayLevel(int? levelNumber);

		void ShowToast(string message);
	}

	public interface IMainMenuNavigator
	{
		void PlayLevel(Guid level);
		void OpenLevelSelect();
	}

	/// <summary>
	/// The Main Menu: Play starts the next level to play (Level Select once every level is done), Levels opens Level
	/// Select, and a long press on the title unlocks every level. Settings and How to play are raised as
	/// <see cref="SettingsRequested"/> / <see cref="HowToPlayRequested"/> for the scene to open.
	/// </summary>
	public sealed class MainMenuPresenter : IDisposable
	{
		public const string UnlockAllMessage = "All levels unlocked!";

		private readonly IMainMenuView _view;
		private readonly Progression _progression;
		private readonly IMainMenuNavigator _navigator;

		private bool _isStarted;

		public MainMenuPresenter(IMainMenuView view, Progression progression, IMainMenuNavigator navigator)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_progression = progression ?? throw new ArgumentNullException(nameof(progression));
			_navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
		}

		/// <summary>Settings was tapped. The scene opens the Settings screen.</summary>
		public event Action SettingsRequested;

		/// <summary>How to play was tapped. The scene opens the tutorial.</summary>
		public event Action HowToPlayRequested;

		public void Start()
		{
			if (_isStarted)
			{
				return;
			}

			_isStarted = true;
			_view.PlayClicked += OnPlayClicked;
			_view.LevelsClicked += OnLevelsClicked;
			_view.SettingsClicked += OnSettingsClicked;
			_view.HowToPlayClicked += OnHowToPlayClicked;
			_view.TitleLongPressed += OnTitleLongPressed;
			_progression.Changed += ShowPlayLevel;
			ShowPlayLevel();
		}

		public void Dispose()
		{
			if (!_isStarted)
			{
				return;
			}

			_isStarted = false;
			_view.PlayClicked -= OnPlayClicked;
			_view.LevelsClicked -= OnLevelsClicked;
			_view.SettingsClicked -= OnSettingsClicked;
			_view.HowToPlayClicked -= OnHowToPlayClicked;
			_view.TitleLongPressed -= OnTitleLongPressed;
			_progression.Changed -= ShowPlayLevel;
		}

		private void ShowPlayLevel()
		{
			Guid? next = _progression.NextToPlay();
			_view.ShowPlayLevel(next.HasValue ? _progression.IndexOf(next.Value) + 1 : (int?)null);
		}

		private void OnPlayClicked()
		{
			Guid? next = _progression.NextToPlay();

			if (next.HasValue)
			{
				_navigator.PlayLevel(next.Value);
			}
			else
			{
				_navigator.OpenLevelSelect();
			}
		}

		private void OnLevelsClicked() => _navigator.OpenLevelSelect();

		private void OnSettingsClicked() => SettingsRequested?.Invoke();

		private void OnHowToPlayClicked() => HowToPlayRequested?.Invoke();

		private void OnTitleLongPressed()
		{
			_progression.UnlockAll();
			_view.ShowToast(UnlockAllMessage);
		}
	}
}
