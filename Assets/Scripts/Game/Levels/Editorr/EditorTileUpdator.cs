using Game.Core;
using Game.Tiles;
using Game.UndoSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Levels.Editorr
{
    internal class EditorTileUpdator : MonoBehaviour, IHasEditMode
    {
        [SerializeField, FormerlySerializedAs("tileSelector")] private ITileSelectorContainer tileSelectorContainer;
        [SerializeField] private EditorTileUiManager tileUiManager;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private IUndoSystemContainer undoSystemContainer;

        public bool IsEditMode { get; set; }

        private ITileSelector TileSelector => tileSelectorContainer.Result;
        private IUndoSystem UndoSystem => undoSystemContainer.Result;

        #region Unity

        private void Awake()
        {
            TileSelector.Selected += OnTileSelected;
        }

        private void OnDestroy()
        {
            if (TileSelector != null)
            {
                TileSelector.Selected -= OnTileSelected;
            }
        }

        #endregion

        #region Events

        private void OnTileSelected(GridVector position)
        {
            if (!IsEditMode)
            {
                return;
            }

            ExpandLevel(ref position);

            TileData data = tileUiManager.Selected;
            TileData prevData = levelManager.GetTileData(position);

            Undoable undoable = new Undoable(
                () => levelManager.UpdateTile(position, data),
                () => levelManager.UpdateTile(position, prevData));
            undoable.Do();

            UndoSystem.Add(undoable);
        }

        private void ExpandLevel(ref GridVector position)
        {
            IReadOnlyLevelData level = levelManager.Level;
            if (LevelShaper.RequiresReshapeToEncapsulatePosition(level.Width, level.Depth, position))
            {
                LevelData newLevel = LevelShaper.EncapsulatePosition(level, position, out GridVector offset);
                position += offset;
                levelManager.SetLevelAfterResize(newLevel, offset);
            }
        }

        #endregion
    }
}
