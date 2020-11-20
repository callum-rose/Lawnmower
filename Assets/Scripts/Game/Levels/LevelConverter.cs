using Core;
using Game.Core;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
    [CreateAssetMenu(fileName = nameof(LevelConverter), menuName = SONames.GameDir + nameof(LevelConverter))]
    internal class LevelConverter : ScriptableObject
    {
        [SerializeField, AssetsOnly] private TilePrefabsHolder tilePrefabs;

        public void ConvertTilesToLevelData(LevelData input, ReadOnlyTiles tiles, GridVector mowerStartPosition)
        {
            input.Resize(tiles.Width, tiles.Depth);
            input.StartPosition = mowerStartPosition;

            for (int y = 0; y < tiles.Depth; y++)
            {
                for (int x = 0; x < tiles.Width; x++)
                {
                    Tile tile = tiles.GetTile(x, y);
                    TileData data = ConvertTileToData(tile);
                    input.SetTile(x, y, data);
                }
            }
        }

        private TileData ConvertTileToData(Tile tile)
        {
            TileType type = tilePrefabs.GetTileTypeForTile(tile);
            BaseTileSetupData setupData = CreateDataFor(tile);
            return TileData.Factory.Create(type, setupData);
        }

        private static BaseTileSetupData CreateDataFor(Tile tile)
        {
            switch (tile)
            {
                case GrassTile gt:
                    return new GrassTileSetupData(gt.GrassHeight);

                case StoneTile st:
                default:
                    return null;
            }
        }
    }
}
