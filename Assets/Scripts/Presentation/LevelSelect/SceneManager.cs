using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using Lawnmower.Presenters;
using UnityEngine;

namespace LevelSelect
{
	/// <summary>Composition root for the LevelSelect scene.</summary>
	internal class SceneManager : MonoBehaviour
	{
		[SerializeField] private LevelTileManager levelTileManager;
		[SerializeField] private CurrentMowerManager currentMowerManager;
		[SerializeField] private LevelDataManager levelDataManager;

		private LevelSelectPresenter _presenter;

		#region Unity

		private void Start()
		{
			_presenter = new LevelSelectPresenter(
				levelTileManager,
				new LevelDataManagerCatalog(levelDataManager),
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
		}
	}
}
