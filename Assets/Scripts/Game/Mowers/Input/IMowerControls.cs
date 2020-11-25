using Game.Core;
using System;

namespace Game.Mowers.Input
{
    internal interface IMowerControls
    {
        event Action<GridVector> Moved;
    }

    [Serializable]
    internal class IMowerControlsContainer : IUnifiedContainer<IMowerControls>
    { }

}
