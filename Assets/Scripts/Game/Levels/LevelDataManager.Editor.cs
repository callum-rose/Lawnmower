#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Game.Levels
{
	internal partial class LevelDataManager
	{
		/// <summary>Level assets in the project that aren't in <see cref="levelDatas"/>. Shown by LevelDataManagerEditor.</summary>
		internal IReadOnlyList<LevelData> NotIncludedLevels => _notIncludedLevels ??= new List<LevelData>();

		private void OnValidate()
		{
			UpdateNotIncludedLevels();
		}

		internal void UpdateNotIncludedLevels()
		{
			_notIncludedLevels = AssetDatabase.FindAssets("t:LevelData")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<LevelData>)
				.Where(l => l != null)
				.Except(levelDatas ?? new LevelData[0])
				.ToList();
		}

		internal void AddNotIncludedLevels()
		{
			UpdateNotIncludedLevels();

			List<LevelData> tempLevels = (levelDatas ?? new LevelData[0]).ToList();
			tempLevels.AddRange(_notIncludedLevels);
			levelDatas = tempLevels.ToArray();
			EditorUtility.SetDirty(this);

			UpdateNotIncludedLevels();
		}
	}
}
#endif
