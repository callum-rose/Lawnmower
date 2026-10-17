using System;
using System.Linq;
using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using Game.Tutorial;
using Lawnmower.Presenters;
using Lawnmower.Screens;
using Lawnmower.Services;
using UnityEngine;

namespace MainMenu
{
	/// <summary>Composition root for the MainMenu scene.</summary>
	public class MainMenuSceneManager : MonoBehaviour
	{
		[SerializeField] private MainMenuView view;
		[SerializeField] private LevelDataManager levelDataManager;
		[SerializeField] private CurrentMowerManager currentMowerManager;

		private MainMenuPresenter _presenter;

		internal MainMenuPresenter Presenter => _presenter;

		#region Unity

		private void Start()
		{
			_presenter = new MainMenuPresenter(view, GameServices.Progression,
				new MainMenuNavigator(levelDataManager, currentMowerManager));
			_presenter.SettingsRequested += OpenSettings;
			_presenter.HowToPlayRequested += OpenHowToPlay;
			_presenter.Start();

#if DEVELOPMENT_BUILD && !UNITY_EDITOR
			if (Lawnmower.Diagnostics.PerfExperiments.TryTakeAutoLevel(out int autoLevel))
			{
				ViewManager.Instance.Load(UnityScene.Game,
					new GameSetupPassThroughData(currentMowerManager.GetCurrent(), levelDataManager.GetLevel(autoLevel).LevelData));
			}
#endif
		}

		private void OnDestroy()
		{
			if (_presenter == null)
			{
				return;
			}

			_presenter.SettingsRequested -= OpenSettings;
			_presenter.HowToPlayRequested -= OpenHowToPlay;
			_presenter.Dispose();
		}

		#endregion

		private static void OpenSettings() => SettingsOverlay.Show();

		private static void OpenHowToPlay() => TutorialEntry.PlayTutorial();

		private sealed class MainMenuNavigator : IMainMenuNavigator
		{
			private readonly LevelDataManager _levels;
			private readonly CurrentMowerManager _mowers;

			public MainMenuNavigator(LevelDataManager levels, CurrentMowerManager mowers)
			{
				_levels = levels;
				_mowers = mowers;
			}

			public void PlayLevel(Guid level)
			{
				LevelData data = _levels.Levels.FirstOrDefault(l => l != null && l.Id == level);

				if (data == null)
				{
					Debug.LogError($"[MainMenu] Level {level} isn't in the level list; opening Level Select instead.");
					OpenLevelSelect();
					return;
				}

				ViewManager.Instance.Load(UnityScene.Game, new GameSetupPassThroughData(_mowers.GetCurrent(), data));
			}

			public void OpenLevelSelect() => ViewManager.Instance.Load(UnityScene.LevelSelect);
		}
	}
}
