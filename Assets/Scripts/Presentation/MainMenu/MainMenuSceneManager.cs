using System;
using System.Linq;
using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using Lawnmower.Presenters;
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

		#region Placeholder hooks (wired at Final integration)

		// TODO(Final): open G4's Settings prefab here.
		private void OpenSettings()
		{
			Debug.Log("[MainMenu] Settings requested: not wired yet (Final integration opens G4's Settings prefab).");
		}

		// TODO(Final): open G3's tutorial here (the same flow as Settings > Replay tutorial).
		private void OpenHowToPlay()
		{
			Debug.Log("[MainMenu] How to play requested: not wired yet (Final integration opens G3's tutorial).");
		}

		#endregion

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
