using Game.Core;
using System;
using IUnified;

namespace Game.Mowers.Input
{
    internal interface IMowerMovementGestureHandler
    {
        event Action<GridVector> Move;
    }

    [Serializable]
    class IMowerMovementGestureHandlerContainer : IUnifiedContainer<IMowerMovementGestureHandler>
    {

    }
}