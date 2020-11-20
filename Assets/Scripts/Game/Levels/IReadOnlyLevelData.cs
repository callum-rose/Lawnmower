using Game.Core;
using Game.Tiles;

namespace Game.Levels
{
    internal interface IReadOnlyLevelData
    {
        int Depth { get; }
        int Width { get; }
        GridVector StartPosition { get; }

        TileData GetTile(GridVector position);
        TileData GetTile(int x, int y);
    }
}
