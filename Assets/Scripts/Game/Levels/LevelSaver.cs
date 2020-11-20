#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector;
using UnityEngine;
using System.IO;
using Core;
using Game.Core;
using Game.Tiles;

namespace Game.Levels
{
    [CreateAssetMenu(fileName = nameof(LevelSaver), menuName = SONames.GameDir + nameof(LevelSaver))]
    internal class LevelSaver : ScriptableObject
    {
        [SerializeField, AssetsOnly] private LevelConverter levelConverter;
        [SerializeField, FolderPath] private string savePath;
        [SerializeField] private string fileName = "NewLevel";

        public void Save_Editor(ReadOnlyTiles tiles, GridVector mowerPosition)
        {
            LevelData newLevel = CreateInstance<LevelData>();
            levelConverter.ConvertTilesToLevelData(newLevel, tiles, mowerPosition);

            LevelData trimmedLevel = LevelShaper.TrimExcess(newLevel);

            AssetDatabase.CreateAsset(trimmedLevel, Path.Combine(savePath, $"{fileName}.asset"));
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = trimmedLevel;
        }
    }
}
#endif
