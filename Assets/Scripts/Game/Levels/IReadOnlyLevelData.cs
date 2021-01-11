using System.Collections.Generic;
using Game.Core;
using Game.Tiles;

namespace Game.Levels
{
    internal interface IReadOnlyLevelData : IEnumerable<Tilee>
    {
        int Depth { get; }
        int Width { get; }
        GridVector StartPosition { get; }

        Tilee GetTile(GridVector position);
        Tilee GetTile(int x, int y);
    }
}
