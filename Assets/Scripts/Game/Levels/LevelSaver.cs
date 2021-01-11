#if UNITY_EDITOR

using UnityEditor;
using Sirenix.OdinInspector;
using UnityEngine;
using System.IO;
using Core;
using Game.Core;
using Game.Tiles;
using Utils;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelSaver), menuName = SONames.GameDir + nameof(LevelSaver))]
	internal class LevelSaver : ScriptableObject
	{
		[SerializeField, AssetsOnly] private LevelConverter levelConverter;
		[SerializeField, FolderPath] private string savePath;
		[SerializeField] private string fileName = "NewLevel";

		public void Save_Editor(IReadOnlyLevelData levelData, GridVector mowerPosition)
		{
			string path = Path.Combine(savePath, $"{fileName}.asset");

			if (AssetDatabase.LoadAssetAtPath<Object>(path) != default &&
			    !EditorUtility.DisplayDialog($"Overwrite {fileName}?", "Overwrite", "Yes", "No"))
			{
				return;
			}

			LevelData newLevel = CreateInstance<LevelData>();
			Loops.TwoD(levelData.Width, levelData.Depth, (x, y) => newLevel.SetTile(x, y, levelData.GetTile(x, y)));

			LevelData trimmedLevel = LevelShaper.TrimExcess(newLevel);

			AssetDatabase.CreateAsset(trimmedLevel, path);
			AssetDatabase.SaveAssets();

			EditorUtility.FocusProjectWindow();
			Selection.activeObject = trimmedLevel;
		}
	}
}
#endif