#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;

namespace Game.Levels
{
	internal partial class LevelDataManager
	{
		#region Odin

		[Button("Update Not Included Levels", ButtonSizes.Large), PropertyOrder(3), HorizontalGroup("Buttons"), GUIColor("@UnityEngine.Color.green")]
		private void UpdateNotIncludedLevels()
		{
			_notIncludedLevels = AssetDatabase.FindAssets("t:LevelData")
				.Select(id => AssetDatabase.GUIDToAssetPath(id))
				.Select(path => AssetDatabase.LoadAssetAtPath<LevelData>(path))
				.Except(levelDatas)
				.ToList();
		}

		[Button(ButtonSizes.Large), PropertyOrder(3), HorizontalGroup("Buttons"), GUIColor("@UnityEngine.Color.blue")]
		private void AddNotIncludedLevels()
		{
			List<LevelData> tempLevels = levelDatas.ToList();
			tempLevels.AddRange(_notIncludedLevels);
			levelDatas = tempLevels.ToArray();

			UpdateNotIncludedLevels();
		}

		#endregion
	}
}

#endif