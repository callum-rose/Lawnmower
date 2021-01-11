using UnityEngine;
using BalsamicBits.Extensions;
using Game.Core;
using Game.Tiles;

namespace Game.Levels
{
    internal static class LevelShaper
    {
        public static LevelData TrimExcess(IReadOnlyLevelData level)
        {
            int lowEmptyRowsCount = 0;
            for (int y = 0; y < level.Depth; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    if (level.GetTile(x, y).GetType() != typeof(EmptyTile))
                    {
                        goto ExitLowRowsLoop;
                    }
                }

                lowEmptyRowsCount++;
            }
ExitLowRowsLoop:

            int lowEmptyColsCount = 0;
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Depth; y++)
                {
                    if (level.GetTile(x, y).GetType() != typeof(EmptyTile))
                    {
                        goto ExitLowColsLoop;
                    }
                }

                lowEmptyColsCount++;
            }
ExitLowColsLoop:

            int highEmptyRowsCount = 0;
            for (int y = level.Depth - 1; y >= 0; y--)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    if (level.GetTile(x, y).GetType() != typeof(EmptyTile))
                    {
                        goto ExitHighRowsLoop;
                    }
                }

                highEmptyRowsCount++;
            }
ExitHighRowsLoop:

            int highEmptyColsCount = 0;
            for (int x = level.Width - 1; x >= 0; x--)
            {
                for (int y = 0; y < level.Depth; y++)
                {
                    if (level.GetTile(x, y).GetType() != typeof(EmptyTile))
                    {
                        goto ExitHighColsLoop;
                    }
                }

                highEmptyColsCount++;
            }
ExitHighColsLoop:

            Tilee[,] tiles = new Tilee[level.Width, level.Depth];
            Utils.Loops.TwoD(level.Width, level.Depth, (x, y) => tiles[x, y] = level.GetTile(x, y));

            tiles = tiles
                .Offset(-lowEmptyColsCount, -lowEmptyRowsCount)
                .Resize(level.Width - (lowEmptyColsCount + highEmptyColsCount), level.Depth - (lowEmptyRowsCount + highEmptyRowsCount));

            LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.Init(tiles, level.StartPosition - new GridVector(lowEmptyColsCount, lowEmptyRowsCount));
            return newLevel;
        }

        public static bool RequiresReshapeToEncapsulatePosition(int width, int depth, GridVector position)
        {
            return position.x < 0 || position.x >= width || position.y < 0 || position.y >= depth;
        }

        public static LevelData EncapsulatePosition(IReadOnlyLevelData level, GridVector position, out GridVector offset)
        {
            Tilee[,] tiles = new Tilee[level.Width, level.Depth];
            Utils.Loops.TwoD(level.Width, level.Depth,
                (x, y) => tiles[x, y] = level.GetTile(x, y));

            int newWidth = Mathf.Max(level.Width, level.Width - position.x, position.x + 1);
            int newDepth = Mathf.Max(level.Depth, level.Depth - position.y, position.y + 1);

            Tilee[,] newTiles = tiles.Resize(newWidth, newDepth, new EmptyTile());
            GridVector newMowerStartPosition = level.StartPosition;
            if (position.x < 0 || position.y < 0)
            {
                offset = new GridVector(Mathf.Max(-position.x, 0), Mathf.Max(-position.y, 0));
                newTiles = newTiles.Offset(offset.x, offset.y, new EmptyTile());
                newMowerStartPosition += offset;
            }
            else
            {
                offset = new GridVector(0, 0);
            }

            LevelData newLevel = ScriptableObject.CreateInstance<LevelData>();
            newLevel.Init(newTiles, newMowerStartPosition);
            return newLevel;
        }
    }
}
