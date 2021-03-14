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
                GridVector prevPosition = _movementManager.MowerPosition;
                
                _movementManager._mowerMover.Move(_targetPosition, false);
                
                _movementManager.Moved.Invoke(
                    prevPosition,
                    _targetPosition,
                    (Xor)false);
            }

            public void Undo()
            {
                _movementManager.Moved.Invoke(
                    _movementManager.MowerPosition,
                    _previousPosition,
                    (Xor)true);

                _movementManager._mowerMover.Move(_previousPosition, true);
            }
        }
    }
}
