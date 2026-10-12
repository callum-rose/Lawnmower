using Game.Levels;
using System;
using System.Collections.Generic;
using BalsamicBits.Extensions;
using UnityEngine;

namespace LevelSelect
{
	internal class LevelTileManager : MonoBehaviour
	{
		[SerializeField] private LevelDataManager levelDataManager;
		[SerializeField] private LevelTile levelTilePrefab;
		[SerializeField] private Transform tileContainer;

		public event Action<IReadOnlyLevelData> LevelSelected;

		private void Awake()
		{
			transform.DestroyAllChildren();
		}

		private void Start()
		{
			IList<LevelInfo> list = levelDataManager.GetAllLevels();
			for (int i = 0; i < list.Count; i++)
			{
				LevelInfo levelInfo = list[i];
				
				LevelTile newTile = Instantiate(levelTilePrefab, tileContainer);
				newTile.Setup(GetLevelNumberFor(i), levelInfo.Locked);

				newTile.Clicked += () => OnTileClicked(levelInfo);
			}
		}

		private void OnTileClicked(LevelInfo levelInfo)
		{
			if (levelInfo.Locked)
			{
				return;
			}
			
			LevelSelected!.Invoke(levelInfo.LevelData);
		}

		private static int GetLevelNumberFor(int levelIndex)
		{
			return levelIndex + 1;
		}
	}
}