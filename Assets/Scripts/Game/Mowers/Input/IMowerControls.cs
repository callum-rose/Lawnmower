using Game.Core;
using System;
using IUnified;

namespace Game.Mowers.Input
{
    public interface IMowerControls
    {
        event Action<GridVector> Moved;
    }

    [Serializable]
    internal class IMowerControlsContainer : IUnifiedContainer<IMowerControls>
    { }

}
