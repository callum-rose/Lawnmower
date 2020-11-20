using Game.Levels;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace LevelSelect
{
    internal class LevelTileManager : MonoBehaviour
    {
        [SerializeField, AssetsOnly] private LevelDataManager levelDataManager;
        [SerializeField, AssetsOnly] private LevelTile levelTilePrefab;
        [SerializeField] private Transform tileContainer;

        public event Action<LevelData> LevelSelected;

        private void Start()
        {
            for (int i = 0; i < levelDataManager.LevelCount; i++)
            {
                var newTile = Instantiate(levelTilePrefab, tileContainer);
                newTile.SetNumber(i);
                newTile.Clicked += OnTileClicked;
            }
        }

        private void OnTileClicked(int tileNumber)
        {
            if (levelDataManager.TryGetLevel(tileNumber, out var level))
            {
                LevelSelected.Invoke(level);
            }
            else
            {
                Debug.Log("Level not unlocked");
            }
        }
    }
}