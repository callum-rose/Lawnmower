using Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace Game.Levels
{
    [CreateAssetMenu(fileName = nameof(LevelDataManager), menuName = SONames.GameDir + nameof(LevelDataManager))]
    internal class LevelDataManager : ScriptableObject
    {
        [SerializeField, OnValueChanged(nameof(UpdateNotIncludedLevels)), AssetsOnly, ListDrawerSettings(Expanded = true, ShowIndexLabels = true), PropertyOrder(1)]
        private LevelData[] levelDatas;

        [ShowInInspector, PropertyOrder(2), BoxGroup("InGameData")]
        public int LevelsCompleted => _levelsCompleted;

        [ShowInInspector, ListDrawerSettings(Expanded = true, IsReadOnly = true), ReadOnly, PropertyOrder(4)]
        private List<LevelData> notIncludedLevels;

        [ShowInInspector, ReadOnly]
        [BoxGroup("InGameData")]
        private int _lastLevel = -1;

        private int _levelsCompleted = -1;

        public int LevelCount => levelDatas.Length;

        #region Unity

        private void Awake()
        {
            if (!PersistantData.Level.LevelsCompleted.TryLoad(out _levelsCompleted))
            {
                PersistantData.Level.LevelsCompleted.Save(0);
            }
        }

        private void OnEnable()
        {
            UpdateNotIncludedLevels();
        }

        #endregion

        #region API

        public bool TryGetLevel(int index, out LevelData level)
        {
            if (index > _levelsCompleted + 1)
            {
                level = null;
                return false;
            }

            level = levelDatas[index];
            return true;
        }

        public int GetLevelIndex(LevelData level)
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
            _levelsCompleted = index;
            PersistantData.Level.LevelsCompleted.Save(_levelsCompleted);
        }

        #endregion

        #region Odin

        [Button("Update Not Included Levels"), PropertyOrder(3)]
        private void UpdateNotIncludedLevels()
        {
            notIncludedLevels = AssetDatabase.FindAssets("t:LevelData")
               .Select(id => AssetDatabase.GUIDToAssetPath(id))
               .Select(path => AssetDatabase.LoadAssetAtPath<LevelData>(path))
               .Except(levelDatas)
               .ToList();
        }

        
        [Button, PropertyOrder(3)]
        private void AddNotIncludedLevels()
        {
            var tempLevels = levelDatas.ToList();
            tempLevels.AddRange(notIncludedLevels);
            levelDatas = tempLevels.ToArray();

            UpdateNotIncludedLevels();
        }

        #endregion
    }
}
