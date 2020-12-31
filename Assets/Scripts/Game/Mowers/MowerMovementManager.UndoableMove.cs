using Game.Core;
using Game.UndoSystem;

namespace Game.Mowers
{
    public partial class MowerMovementManager
    {
        private class UndoableMove : IUndoable
        {
            private readonly MowerMovementManager _movementController;
            private readonly GridVector _targetPosition;
            private readonly GridVector _previousPosition;

            public UndoableMove(MowerMovementManager movementController, GridVector targetPosition, GridVector previousPosition)
            {
                _movementController = movementController;
                _targetPosition = targetPosition;
                _previousPosition = previousPosition;
            }

            public void Do()
            {
                _movementController.Moved.Invoke(
                    _movementController.mowerMover.CurrentPosition,
                    _targetPosition,
                    (Xor)false);

                _movementController.mowerMover.Move(_targetPosition);
            }

            public void Undo()
            {
                _movementController.Moved.Invoke(
                    _movementController.mowerMover.CurrentPosition,
                    _previousPosition,
                    (Xor)true);

                _movementController.mowerMover.Move(_previousPosition);
            }
        }
    }
}
