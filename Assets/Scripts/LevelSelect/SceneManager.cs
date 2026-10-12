using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using UnityEngine;

namespace LevelSelect
{
	internal class SceneManager : MonoBehaviour
	{
		[SerializeField] private LevelTileManager levelTileManager;
		[SerializeField] private CurrentMowerManager currentMowerManager;

		#region Unity

		private void Awake()
		{
			levelTileManager.LevelSelected += OnLevelSelected;
		}

		private void OnDestroy()
		{
			levelTileManager.LevelSelected -= OnLevelSelected;
		}

		#endregion

		#region Events

		private void OnLevelSelected(LevelData level)
		{
			MowerData currentMower = currentMowerManager.GetCurrent();
			GameSetupPassThroughData data = new GameSetupPassThroughData(currentMower, level);
			ViewManager.Instance.Load(UnityScene.Game, data);
		}

		#endregion
	}
}