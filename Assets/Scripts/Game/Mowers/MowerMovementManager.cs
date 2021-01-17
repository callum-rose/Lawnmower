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
        [SerializeField] private TileInteractor tileInteractor;

        /// <summary>
        /// First parameter is the previous position, second is target position
        /// </summary>
#pragma warning disable 67
        public event UndoableAction<GridVector, GridVector> Moved, Bumped;
#pragma warning restore 67

        public GridVector MowerPosition => _mowerMover.CurrentPosition.Value;

        public bool IsRunning { get; set; } = false;
        
        private ILevelTraversalChecker _traversalChecker;

        private MowerMover _mowerMover;
        private IMowerControls[] _mowerControls;

        private IUndoSystem _undoManager;
        
        #region Unity

        private void OnDestroy()
        {
            foreach (IMowerControls mc in _mowerControls)
            {
                mc.MovedInDirection -= OnInput;
            }
        }

        #endregion

        #region API

        internal void Construct(TileInteractor tileInteractor) => this.tileInteractor = tileInteractor;

        public void Init(MowerMover mowerMover, IMowerControls[] controls, ILevelTraversalChecker traversalChecker, IUndoSystem undoManager)
        {
            _mowerMover = mowerMover;
            
            _mowerControls = controls;
            foreach (IMowerControls mc in _mowerControls)
            {
                mc.MovedInDirection += OnInput;
            }

            _traversalChecker = traversalChecker;

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
            switch (_traversalChecker.CanTraverseTo(targetPosition))
            {
                case TileTraversalStatus.Yes:
                    action = new UndoableMove(this, targetPosition, MowerPosition);
                    break;

                case TileTraversalStatus.NonTraversable:
                    action = new UndoableBump(this, targetPosition, MowerPosition);
                    break;

                case TileTraversalStatus.OutOfBounds:
                default:
                    return;
            }

            _undoManager.Do(action);
        }

        #endregion
    }
}
