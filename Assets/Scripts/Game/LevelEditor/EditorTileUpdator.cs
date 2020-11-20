using Game.Core;
using Game.Levels;
using Game.Tiles;
using UnityEngine;

namespace Game.LevelEditor
{
    internal class EditorTileUpdator : MonoBehaviour, IHasEditMode
    {
        [SerializeField] private MouseTileSelector tileSelector;
        [SerializeField] private EditorTileUiManager tileUiManager;
        [SerializeField] private LevelManager levelManager;

        public bool IsEditMode { get; set; }

        #region Unity

        private void Awake()
        {
            tileSelector.Selected += OnTileSelected;
        }

        private void OnDestroy()
        {
            tileSelector.Selected -= OnTileSelected;
        }

        #endregion

        #region API

        #endregion

        #region Events

        private void OnTileSelected(GridVector position)
        {
            if (!IsEditMode)
            {
                return;
            }

            TileData data = tileUiManager.Selected;

            IReadOnlyLevelData level = levelManager.Level;
            if (LevelShaper.RequiresReshapeToEncapsulatePosition(level.Width, level.Depth, position))
            { 
                var newLevel = LevelShaper.EncapsulatePosition(level, position, out GridVector offset);
                position += offset;
                levelManager.SetLevel(newLevel, offset);
            }

            levelManager.UpdateTile(position, data);
        }

        #endregion
    }
}
