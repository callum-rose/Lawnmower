using System;
using UnityEngine;
using Sirenix.OdinInspector;
using BalsamicBits.Extensions;
using Core;
using Game.Core;
using Game.Tiles;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Levels
{
    [CreateAssetMenu(fileName = "LevelData", menuName = SONames.GameDir + "Level Data")]
    internal class LevelData : SerializedScriptableObject, IReadOnlyLevelData
    {
        [SerializeField] private Guid id;

        [SerializeField, HideInInspector] private GridVector startPosition = new GridVector(1, 1);

        [SerializeField, PropertyOrder(2),
        OnValueChanged(nameof(OnTileDataChanged), true)]
        private Serialised2dArray<TileData> tiles;

        [HideInInspector]
        public Guid Id => id;

        [ShowInInspector, PropertyOrder(1), MinValue(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
        public int Width => tiles.Width;
        [ShowInInspector, PropertyOrder(1), MinValue(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
        public int Depth => tiles.Depth;

        [ShowInInspector, PropertyOrder(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
        public GridVector StartPosition
        {
            get => startPosition;
            set => startPosition = value;
        }

        #region Unity

        private void Awake()
        {
            if (id == Guid.Empty)
            {
                id = Guid.NewGuid();
            }
        }

        #endregion

        #region API

        public void Init(TileData[,] tiles, GridVector mowerStartPosition)
        {
            StartPosition = mowerStartPosition;

            int width = tiles.GetLength(0);
            int depth = tiles.GetLength(1);

            this.tiles = new Serialised2dArray<TileData>(width, depth, tiles);
        }

        public TileData GetTile(int x, int y)
        {
            return tiles[x, y];
        }

        public TileData GetTile(GridVector position)
        {
            return GetTile(position.x, position.y);
        }

        public void Resize(int newWidth, int newDepth)
        {
            if (newWidth <= 0 || newDepth <= 0)
            {
                return;
            }

            Serialised2dArray<TileData> oldTiles = tiles;
            tiles = null;

            // create new array
            Serialised2dArray<TileData> newTiles = new Serialised2dArray<TileData>(newWidth, newDepth);
            Utils.Loops.TwoD(newWidth, newDepth,
                (x, y) =>
                {
                    newTiles[x, y] = new TileData();
                });

            if (!oldTiles.IsNullOrEmpty())
            {
                // trim if new width is smaller
                int xRange = Mathf.Min(newWidth, Width);
                for (int x = 0; x < xRange; x++)
                {
                    int yRange = Mathf.Min(newDepth, Depth);
                    for (int y = 0; y < yRange; y++)
                    {
                        newTiles[x, y] = oldTiles[x, y];
                    }
                }
            }

            // update values
            tiles = newTiles;
        }

        public void SetTile(int x, int y, TileData data)
        {
            tiles[x, y] = data;
        }

        public void SetTile(GridVector position, TileData data)
        {
            SetTile(position.x, position.y, data);
        }

        public LevelData GetCopy()
        {
            var newLevel = CreateInstance<LevelData>();
            newLevel.tiles = new Serialised2dArray<TileData>(tiles);
            newLevel.startPosition = startPosition;
            newLevel.id = id;
            return newLevel;
        }

        #endregion

        #region Odin
#if UNITY_EDITOR

        private void OnTileDataChanged()
        {
            //Debug.Log("data changed");
        }

        private void ValidateStartPos()
        {
            int x = Mathf.Min(Width - 1, Mathf.Max(0, StartPosition.x));
            int y = Mathf.Min(Depth - 1, Mathf.Max(0, StartPosition.y));
            StartPosition = new GridVector(x, y);
        }

        [Button]
        private void SelectInProjectileWindow()
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = this;
        }

#endif
        #endregion

        #region Methods



        #endregion
    }
}
