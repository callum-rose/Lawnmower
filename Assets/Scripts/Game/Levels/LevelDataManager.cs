using Core;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelDataManager), menuName = SONames.GameDir + nameof(LevelDataManager))]
	internal partial class LevelDataManager : ScriptableObject
	{
		[ShowInInspector, PropertyOrder(5), TitleGroup("In Game Data")]
		public int LevelsCompleted { get; private set; }

		[SerializeField, PropertyOrder(5)] private bool unlockAllLevels;

		[SerializeField]
#if UNITY_EDITOR
		[OnValueChanged(nameof(UpdateNotIncludedLevels)), AssetsOnly,
		 ListDrawerSettings(Expanded = true, ShowIndexLabels = true), PropertyOrder(1)]
#endif
		private LevelData[] levelDatas;

		[ShowInInspector, ListDrawerSettings(Expanded = true, IsReadOnly = true), ReadOnly, PropertyOrder(4)]
		private List<LevelData> _notIncludedLevels;

		public int LevelCount => levelDatas.Length;

		#region Unity

		private void Awake()
		{
			LevelsCompleted = PersistantData.Level.LevelsCompleted.Load();

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

		public bool TryGetLevel(int index, out LevelData level)
		{
			if (index < 0 || index >= levelDatas.Length)
			{
				level = null;
				return false;
			}

			level = LevelData.CreateFrom(levelDatas[index]);
			return true;
		}

		public bool IsLevelLocked(int index) => index > LevelsCompleted;

		public int GetLevelIndex(IReadOnlyLevelData level)
		{
			for (int i = 0; i < levelDatas.Length; i++)
			{
				if (levelDatas[i].Id == level.Id)
				{
					return i;
				}
			}

			throw new System.Exception("Level index not found");
		}

		public void SetLevelCompleted(int index)
		{
			LevelsCompleted = index + 1;
			PersistantData.Level.LevelsCompleted.Save(LevelsCompleted);
		}

		#endregion
	}
}