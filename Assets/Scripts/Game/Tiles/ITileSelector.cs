using System;
using Game.Core;
using IUnified;

namespace Game.Tiles
{
    internal interface ITileSelector
    {
        event Action<GridVector> Selected;
    }

    [Serializable]
    class ITileSelectorContainer : IUnifiedContainer<ITileSelector>
    {

    }
}
