using Core;
using Game.Core;
using Game.Levels;
using UnityEngine;

namespace LevelSelect
{
    internal class SceneManager : MonoBehaviour
    {
        [SerializeField] private LevelTileManager levelTileManager;

        #region Unity

        private void Awake()
        {
            levelTileManager.LevelSelected += OnLevelSelected;
        }

        private void OnDestroy()
        {
            levelTileManager.LevelSelected -= OnLevelSelected;
        }

        #endregion

        #region Events

        private void OnLevelSelected(LevelData level)
        {
            GameSetupPassThroughData data = new GameSetupPassThroughData
            {
                MowerId = null,
                Level = level
            };
            ViewManager.Instance.Load(UnityScene.Game, data);
        }

        #endregion
    }
}
