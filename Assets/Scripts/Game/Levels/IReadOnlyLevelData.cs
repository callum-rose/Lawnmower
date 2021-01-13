using System;
using System.Collections.Generic;
using Game.Core;
using Game.Tiles;
using IUnified;

namespace Game.Levels
{
    internal interface IReadOnlyLevelData : IEnumerable<Tile>
    {
        Guid Id { get; }
        
        int Depth { get; }
        int Width { get; }
        GridVector StartPosition { get; }

        Tile GetTile(GridVector position);
        Tile GetTile(int x, int y);
    }

    [Serializable]
    internal class IReadOnlyLevelDataContainer : IUnifiedContainer<IReadOnlyLevelData>
    {
        
    }
}
