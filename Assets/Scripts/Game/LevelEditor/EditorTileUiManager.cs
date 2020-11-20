using Game.Tiles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.LevelEditor
{
    public class EditorTileUiManager : MonoBehaviour
    {
        [SerializeField] private TileIconCapturer iconCapturerPrefab;
        [SerializeField] private Transform iconCapturerContainer;
        [SerializeField] private TileUiIcon iconPrefab;
        [SerializeField] private Transform iconUiContainer;

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

        private List<TileIconCapturer> _tileIconCapturers = new List<TileIconCapturer>(Enum.GetNames(typeof(TileType)).Length);

        public TileData Selected { get; private set; }

        private void Awake()
        {
            for (int i = 0; i < IconData.Count; i++)
            {
                TileData data = IconData[i];

                TileIconCapturer newIconCapturer = Instantiate(iconCapturerPrefab, iconCapturerContainer);
                newIconCapturer.transform.localPosition = Vector3.right * i * 3;
                RenderTexture renderTexture = newIconCapturer.Setup(data);

                TileUiIcon newIcon = Instantiate(iconPrefab, iconUiContainer);
                newIcon.Setup(data, renderTexture);

                newIcon.Clicked += OnIconClicked;

                _tileIconCapturers.Add(newIconCapturer);
            }
        }

        private void OnIconClicked(TileData data)
        {
            Selected = data;
        }

        private void Start()
        {
            foreach (var ic in _tileIconCapturers)
            {
                ic.Render();
            }
        }
    }
}