#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Levels
{
	internal partial class LevelDataManager
	{
		[ContextMenu("Update Not Included Levels")]
		private void UpdateNotIncludedLevels()
		{
			_notIncludedLevels = AssetDatabase.FindAssets("t:LevelData")
				.Select(id => AssetDatabase.GUIDToAssetPath(id))
				.Select(path => AssetDatabase.LoadAssetAtPath<LevelData>(path))
				.Except(levelDatas)
				.ToList();
		}

		[ContextMenu("Add Not Included Levels")]
		private void AddNotIncludedLevels()
		{
			UpdateNotIncludedLevels();

			List<LevelData> tempLevels = levelDatas.ToList();
			tempLevels.AddRange(_notIncludedLevels);
			levelDatas = tempLevels.ToArray();
			EditorUtility.SetDirty(this);

			UpdateNotIncludedLevels();
		}
	}
}
#endif
