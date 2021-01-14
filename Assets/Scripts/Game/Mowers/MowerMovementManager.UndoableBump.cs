using Game.Core;
using Game.UndoSystem;

namespace Game.Mowers
{
    public partial class MowerMovementManager
    {
        private class UndoableBump : IUndoable
        {
            private readonly MowerMovementManager _movementController;
            private readonly GridVector _targetPosition;
            private readonly GridVector _previousPosition;

            public UndoableBump(MowerMovementManager movementController, GridVector targetPosition, GridVector previousPosition)
            {
                _movementController = movementController;
                _targetPosition = targetPosition;
                _previousPosition = previousPosition;
            }

            public void Do()
            {
                _movementController.Bumped.Invoke(
                    _movementController.MowerPosition,
                    _targetPosition,
                    (Xor)false);

                _movementController._mowerMover.Bump(_targetPosition);
            }

            public void Undo()
            {
                _movementController.Bumped.Invoke(
                    _movementController.MowerPosition,
                    _previousPosition,
                    (Xor)true);

                _movementController._mowerMover.Bump(_previousPosition);
            }
        }
    }
}
