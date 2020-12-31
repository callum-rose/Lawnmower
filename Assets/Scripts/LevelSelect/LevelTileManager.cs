using Game.Levels;
using Sirenix.OdinInspector;
using System;
using BalsamicBits.Extensions;
using UnityEngine;

namespace LevelSelect
{
	internal class LevelTileManager : MonoBehaviour
	{
		[SerializeField, AssetsOnly] private LevelDataManager levelDataManager;
		[SerializeField, AssetsOnly] private LevelTile levelTilePrefab;
		[SerializeField] private Transform tileContainer;

		public event Action<LevelData> LevelSelected;

		private void Awake()
		{
			transform.DestroyAllChildren();
		}

		private void Start()
		{
			for (int i = 0; i < levelDataManager.LevelCount; i++)
			{
				LevelTile newTile = Instantiate(levelTilePrefab, tileContainer);
				newTile.Setup(GetLevelNumber(i), levelDataManager.IsLevelLocked(i));

				int tempI = i;
				newTile.Clicked += () => OnTileClicked(tempI);
			}
		}

		private void OnTileClicked(int index)
		{
			if (levelDataManager.IsLevelLocked(index))
			{
				Debug.Log("Level index " + index + " is not unlocked");
				return;
			}

			LevelData level;
			if (!levelDataManager.TryGetLevel(index, out level))
			{
				Debug.LogError("Level with index " + index + " does not exist");
				return;
			}
			
			LevelSelected.Invoke(level);
		}

		private static int GetLevelNumber(int levelIndex)
		{
			return levelIndex + 1;
		}
	}
}