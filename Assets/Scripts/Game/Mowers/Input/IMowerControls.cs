using Game.Core;
using System;
using IUnified;

namespace Game.Mowers.Input
{
    public interface IMowerControls
    {
        event Action<GridVector> MovedInDirection;
    }

    [Serializable]
    internal class IMowerControlsContainer : IUnifiedContainer<IMowerControls>
    { }
}
