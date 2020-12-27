using Game.Core;
using System;
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
