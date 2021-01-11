using System.Collections.Generic;
using Game.Tiles;
using UI.Buttons;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Levels.Editorr
{
    internal class EditorTileUiManager : MonoBehaviour
    {
        [SerializeField] private Button buttonPrefab;
        [SerializeField, FormerlySerializedAs("iconUiContainer")] private Transform buttonContainer;

        public Tilee Selected { get; private set; }

        #region Unity

        private void Start()
        {
            foreach (Tilee tile in TileeStatics.AllTileConfigurations)
            {
                Button newButton = Instantiate(buttonPrefab, buttonContainer);
                ButtonInfo info = new ButtonInfo(
                    tile.ToString(), 
                    action: () => OnIconClicked(tile));
                newButton.Init(info);
            }
        }

        #endregion

        #region Events

        private void OnIconClicked(Tilee tile)
        {
            Selected = tile;
        }

        #endregion
    }
}