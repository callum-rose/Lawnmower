using Game.Core;
using System;

namespace Game.Tiles
{
    internal interface ITileSelector
    {
        event Action<GridVector> Selected;
    }
}
