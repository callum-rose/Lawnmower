using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers.Input;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Game.Mowers
{
    [CreateAssetMenu(fileName = nameof(MowerMovementManager), menuName = SONames.GameDir + nameof(MowerMovementManager))]
    public partial class MowerMovementManager : ScriptableObject, IMowerRunnable
    {
        [SerializeField] private LevelTraversalChecker traversalChecker;
        [FormerlySerializedAs("levelInteractor")] [SerializeField] private TileInteractor tileInteractor;

        /// <summary>
        /// First parameter is the previous position, second is target position
        /// </summary>
        public event UndoableAction<GridVector, GridVector> Moved, Bumped;
        
        public GridVector MowerPosition => _mowerMover.CurrentPosition.Value;

        public bool IsRunning { get; set; } = false;
        
        private MowerMoverr _mowerMover;
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

        public void Init(MowerMoverr mowerMover, IMowerControls[] controls, IUndoSystem undoManager)
        {
            _mowerMover = mowerMover;

            _mowerControls = controls;
            foreach (IMowerControls mc in _mowerControls)
            {
                mc.Moved += OnInput;
            }

            _undoManager = undoManager;
        }

        [Button(Expanded = true)]
        public void SetPosition(GridVector position)
        {
            _mowerMover.Move(position);
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

            GridVector targetPosition = MowerPosition + direction;

            IUndoable action;
            switch (traversalChecker.CanTraverseTo(targetPosition))
            {
                case LevelTraversalChecker.CheckValue.Yes:
                    action = new UndoableMove(this, targetPosition, MowerPosition);
                    break;

                case LevelTraversalChecker.CheckValue.NonTraversableTile:
                    action = new UndoableBump(this, targetPosition, MowerPosition);
                    break;

                case LevelTraversalChecker.CheckValue.OutOfBounds:
                default:
                    return;
            }

            _undoManager.Do(action);
        }

        #endregion
    }
}
