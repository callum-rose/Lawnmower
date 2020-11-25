using Game.Core;
using System;

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