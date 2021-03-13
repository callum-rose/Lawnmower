#if UNITY_EDITOR

using System.IO;
using Core;
using Game.Core;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Utils;

namespace Game.Levels.Editorr
{
	[CreateAssetMenu(fileName = nameof(LevelSaver), menuName = SoNames.GameDir + nameof(LevelSaver))]
	internal class LevelSaver : ScriptableObject
	{
		[SerializeField, FolderPath] private string savePath;
		[SerializeField] private string fileName = "NewLevel";

		public void Save_Editor(EditableLevelData levelData, GridVector mowerPosition)
		{
			string path = Path.Combine(savePath, $"{fileName}.asset");

			if (AssetDatabase.LoadAssetAtPath<Object>(path) != default &&
			    !EditorUtility.DisplayDialog($"Overwrite {fileName}?", "Overwrite", "Yes", "No"))
			{
				return;
			}

			EditableLevelData newLevel = new EditableLevelData();
			Loops.TwoD(levelData.Width, levelData.Depth, (x, y) => newLevel.SetTile(x, y, levelData.GetTile(x, y)));

			EditableLevelData trimmedLevel = LevelShaper.TrimExcess(newLevel);

			LevelData level = LevelData.CreateFrom(trimmedLevel);

			AssetDatabase.CreateAsset(level, path);
			AssetDatabase.SaveAssets();

			EditorUtility.FocusProjectWindow();
			Selection.activeObject = level;
		}
	}
}
#endif