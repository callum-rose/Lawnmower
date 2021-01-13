using System;
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
            // TODO
            throw new NotImplementedException();
            
            // if (!IsEditMode)
            // {
            //     return;
            // }
            //
            // ExpandLevel(ref position);

            // Tilee tile = tileUiManager.Selected;
            // Tilee previousTile = levelManager.GetTileData(position);
            //
            // Undoable undoable = new Undoable(
            //     () => levelManager.UpdateTile(position, tile),
            //     () => levelManager.UpdateTile(position, previousTil));
            //
            // UndoSystem.Do(undoable);
        }

        private void ExpandLevel(EditableLevelData level, ref GridVector position)
        {
            // if (!LevelShaper.RequiresReshapeToEncapsulatePosition(level.Width, level.Depth, position))
            // {
            //     return;
            // }
            //
            // EditableLevelData newLevel = LevelShaper.EncapsulatePosition(level, position, out GridVector offset);
            // position += offset;
            // levelManager.SetLevelAfterResize(newLevel, offset);
        }

        #endregion
    }
}
