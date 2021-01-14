using Game.Core;
using Game.UndoSystem;

namespace Game.Mowers
{
    public partial class MowerMovementManager
    {
        private class UndoableMove : IUndoable
        {
            private readonly MowerMovementManager _movementManager;
            private readonly GridVector _targetPosition;
            private readonly GridVector _previousPosition;

            public UndoableMove(MowerMovementManager movementManager, GridVector targetPosition, GridVector previousPosition)
            {
                _movementManager = movementManager;
                _targetPosition = targetPosition;
                _previousPosition = previousPosition;
            }

            public void Do()
            {
                _movementManager.Moved.Invoke(
                    _movementManager.MowerPosition,
                    _targetPosition,
                    (Xor)false);

                _movementManager._mowerMover.Move(_targetPosition);
            }

            public void Undo()
            {
                _movementManager.Moved.Invoke(
                    _movementManager.MowerPosition,
                    _previousPosition,
                    (Xor)true);

                _movementManager._mowerMover.Move(_previousPosition);
            }
        }
    }
}
