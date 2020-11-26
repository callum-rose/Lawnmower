using Game.Tiles;
using System.Collections.Generic;
using UI.Buttons;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Levels.Editor
{
    public class EditorTileUiManager : MonoBehaviour
    {
        [SerializeField] private Button buttonPrefab;
        [SerializeField, FormerlySerializedAs("iconUiContainer")] private Transform buttonContainer;

        private readonly IReadOnlyList<TileData> IconData = new TileData[]
        {
            TileData.Factory.Create(TileType.Empty, null),
            TileData.Factory.Create(TileType.Grass, new GrassTileSetupData(1)),
            TileData.Factory.Create(TileType.Grass, new GrassTileSetupData(2)),
            TileData.Factory.Create(TileType.Grass, new GrassTileSetupData(3)),
            TileData.Factory.Create(TileType.Stone, null),
            TileData.Factory.Create(TileType.Water, null),
            TileData.Factory.Create(TileType.Wood, null),
        };

        public TileData Selected { get; private set; }

        #region Unity

        private void Start()
        {
            foreach (var data in IconData)
            {
                Button newButton = Instantiate(buttonPrefab, buttonContainer);
                ButtonInfo info = new ButtonInfo(
                    data.ToString(), 
                    action: () => OnIconClicked(data));
                newButton.Init(info);
            }
        }

        #endregion

        #region Events

        private void OnIconClicked(TileData data)
        {
            Selected = data;
        }

        #endregion
    }
}