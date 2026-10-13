using Core;
using Lawnmower.Services;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelDataManager), menuName = SoNames.GameDir + nameof(LevelDataManager))]
	internal partial class LevelDataManager : ScriptableObject
	{
		[SerializeField] private LevelData[] levelDatas;

		private List<LevelData> _notIncludedLevels;

		public int LevelCount => levelDatas.Length;

		/// <summary>The level list in play order.</summary>
		public IReadOnlyList<LevelData> Levels => levelDatas;

		#region Unity

		private void Awake()
		{
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

		public bool GetLevelAfter(LevelData level, out LevelInfo levelInfo)
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

		#endregion

		#region Methods

		private LevelInfo GetInfoForLevelAt(int index)
		{
			if (index < levelDatas.Length)
			{
				return new LevelInfo(levelDatas[index], IsLevelLocked(index));
			}
			else
			{
				return default;
			}
		}

		/// <summary>Locks come from the saved progression (levels unlock in order; unlock-all opens them all).</summary>
		private bool IsLevelLocked(int index) => index > 0 && !GameServices.Progression.IsUnlocked(levelDatas[index].Id);

		#endregion
	}
}