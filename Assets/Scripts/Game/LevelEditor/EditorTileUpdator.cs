using Game.Core;
using Game.Levels;
using Game.Tiles;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.LevelEditor
{
    internal class EditorTileUpdator : MonoBehaviour, IHasEditMode
    {
        [SerializeField, FormerlySerializedAs("tileSelector")] private ITileSelectorContainer tileSelectorContainer;
        [SerializeField] private EditorTileUiManager tileUiManager;
        [SerializeField] private LevelManager levelManager;

        public bool IsEditMode { get; set; }

        private ITileSelector TileSelector => tileSelectorContainer.Result;

        #region Unity

        private void Awake()
        {
            TileSelector.Selected += OnTileSelected;
        }

        private void OnDestroy()
        {
            TileSelector.Selected -= OnTileSelected;
        }

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
