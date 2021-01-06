using Game.Core;
using Game.Levels;
using Game.Mowers.Input;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Mowers
{
    public partial class MowerMovementManager : MonoBehaviour, IMowerPosition, IMowerRunnable
    {
        [SerializeField] private MowerMover mowerMover;
        [SerializeField] private LevelTraversalChecker traversalChecker;
        [SerializeField] private LevelInteractor levelInteractor;

        /// <summary>
        /// First parameter is the previous position, second is target position
        /// </summary>
        public event UndoableAction<GridVector, GridVector> Moved, Bumped;

        public GridVector MowerPosition => mowerMover.CurrentPosition;

        public bool IsRunning { get; set; } = false;

        private IMowerControls[] _mowerControls;

        private IUndoSystem _undoManager;

        #region Unity

        private void OnDestroy()
        {
            foreach (IMowerControls mc in _mowerControls)
            {
                mc.Moved -= OnInput;
            }
        }

        #endregion

        #region API

        public void Init(IMowerControls[] controls, Positioner positioner, IUndoSystem undoManager)
        {
            _mowerControls = controls;
            foreach (IMowerControls mc in _mowerControls)
            {
                mc.Moved += OnInput;
            }

            mowerMover.Init(positioner);
            _undoManager = undoManager;
        }

        [Button(Expanded = true)]
        public void SetPosition(GridVector position)
        {
            mowerMover.Move(position);
        }

        #endregion

        #region Events

        private void OnInput(GridVector direction)
        {
            Assert.AreApproximatelyEqual(direction.Magnitude, 1f);

            if (!IsRunning)
            {
                return;
            }

            GridVector targetPosition = mowerMover.CurrentPosition + direction;

            IUndoable action;
            switch (traversalChecker.CanTraverseTo(targetPosition))
            {
                case LevelTraversalChecker.CheckValue.Yes:
                    action = new UndoableMove(this, targetPosition, mowerMover.CurrentPosition);
                    break;

                case LevelTraversalChecker.CheckValue.NonTraversableTile:
                    action = new UndoableBump(this, targetPosition, mowerMover.CurrentPosition);
                    break;

                case LevelTraversalChecker.CheckValue.OutOfBounds:
                default:
                    return;
            }

            action.Do();
            _undoManager.Add(action);
        }

        #endregion
    }
}
