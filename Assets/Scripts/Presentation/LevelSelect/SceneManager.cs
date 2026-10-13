using System;
using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using Lawnmower.Presenters;
using Lawnmower.Services;
using UnityEngine;

namespace LevelSelect
{
	/// <summary>Composition root for the LevelSelect scene.</summary>
	internal class SceneManager : MonoBehaviour
	{
		[SerializeField] private LevelSelectScreenView screen;
		[SerializeField] private CurrentMowerManager currentMowerManager;
		[SerializeField] private LevelDataManager levelDataManager;

		private LevelSelectPresenter _presenter;

		public LevelSelectScreenView Screen => screen;

		#region Unity

		private void Start()
		{
			_presenter = new LevelSelectPresenter(
				screen,
				new ProgressionLevelCatalog(GameServices.Progression),
				new LevelSelectNavigator(levelDataManager, currentMowerManager));
			_presenter.Start();
		}

		private void OnDestroy()
		{
			_presenter?.Dispose();
		}

		#endregion

		private sealed class LevelSelectNavigator : ILevelSelectNavigator
		{
			private readonly LevelDataManager _levels;
			private readonly CurrentMowerManager _mowers;

			public LevelSelectNavigator(LevelDataManager levels, CurrentMowerManager mowers)
			{
				_levels = levels;
				_mowers = mowers;
			}

			public void PlayLevel(int index)
			{
				LevelData level = _levels.GetLevel(index).LevelData;
				ViewManager.Instance.Load(UnityScene.Game, new GameSetupPassThroughData(_mowers.GetCurrent(), level));
			}

			/// <summary>
			/// By name, so this compiles before and after the MainMenu entry is (re-)enabled in <see cref="UnityScene"/>
			/// (G2 owns that change).
			/// </summary>
			public void GoToMainMenu()
			{
				if (Enum.TryParse("MainMenu", out UnityScene mainMenu) && mainMenu != UnityScene.None)
				{
					ViewManager.Instance.Load(mainMenu);
				}
				else
				{
					Debug.LogWarning("[LevelSelect] Back: UnityScene has no MainMenu entry yet, so there's nowhere to go back to.");
				}
			}
		}
	}
}
