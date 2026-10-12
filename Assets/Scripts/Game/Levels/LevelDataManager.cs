using Core;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelDataManager), menuName = SoNames.GameDir + nameof(LevelDataManager))]
	internal partial class LevelDataManager : ScriptableObject
	{
		public int LevelsCompleted { get; private set; }

		[SerializeField] private bool unlockAllLevels;

		[SerializeField] private LevelData[] levelDatas;

		private List<LevelData> _notIncludedLevels;

		public int LevelCount => levelDatas.Length;

		#region Unity

		private void Awake()
		{
			LevelsCompleted = PersistantData.LevelModule.LevelsCompleted.Load();

			if (levelDatas.Any(l => l == null))
			{
				Debug.LogError("Level is null in " + nameof(LevelDataManager));
			}
		}

#if UNITY_EDITOR
		private void OnEnable()
		{
			UpdateNotIncludedLevels();
		}
#endif

		#endregion

		#region API

		public LevelInfo GetLevel(int index)
		{
			return GetInfoForLevelAt(index);
		}
		
		public IList<LevelInfo> GetAllLevels()
		{
			List<LevelInfo> result = new List<LevelInfo>(levelDatas.Length);

			for (int i = 0; i < levelDatas.Length; i++)
			{
				result.Add(GetInfoForLevelAt(i));
			}

			return result;
		}

		public bool GetLevelAfter(IReadOnlyLevelData level, out LevelInfo levelInfo)
		{
			for (int i = 0; i < levelDatas.Length; i++)
			{
				bool isIndexOfInputLevel = levelDatas[i].Id == level.Id;
				if (!isIndexOfInputLevel)
				{
					continue;
				}

				levelInfo = GetInfoForLevelAt(i + 1);
				return !levelInfo.Equals(default);
			}

			levelInfo = default;
			return false;
		}

		public void SetLevelCompleted(IReadOnlyLevelData level)
		{
			for (int i = 0; i < levelDatas.Length; i++)
			{
				bool isIndexOfInputLevel = levelDatas[i].Id == level.Id;
				if (!isIndexOfInputLevel)
				{
					continue;
				}

				LevelsCompleted = i;
					
				PersistantData.LevelModule.LevelsCompleted.Save(LevelsCompleted);
					
				return;
			}
		}

		#endregion

		#region Methods

		private LevelInfo GetInfoForLevelAt(int index)
		{
			if (index < levelDatas.Length)
			{
				LevelData clonedLevel = LevelData.CreateFrom(levelDatas[index]);
				return new LevelInfo(clonedLevel, IsLevelLocked(index));
			}
			else
			{
				return default;
			}
		}

		private bool IsLevelLocked(int index) => index > LevelsCompleted;

		#endregion
	}
}