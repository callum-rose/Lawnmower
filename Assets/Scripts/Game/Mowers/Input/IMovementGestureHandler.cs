using Game.Core;
using System;

namespace Game.Mowers.Input
{
    internal interface IMovementGestureHandler
    {
        event Action<GridVector> Move;
    }

    [Serializable]
    class IMovementGestureHandlerContainer : IUnifiedContainer<IMovementGestureHandler>
    {

    }
}