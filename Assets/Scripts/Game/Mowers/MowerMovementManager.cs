using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers.Input;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Mowers
{
    [CreateAssetMenu(fileName = nameof(MowerMovementManager), menuName = SONames.GameDir + nameof(MowerMovementManager))]
    public partial class MowerMovementManager : ScriptableObject, IMowerRunnable
    {
        [SerializeField] private MowerInputEventChannel mowerInputEventChannel;
        
#pragma warning disable 67
        /// <summary>
        /// First parameter is the previous position, second is target position
        /// </summary>
        public event UndoableAction<GridVector, GridVector> Moved, Bumped;
#pragma warning restore 67

        public GridVector MowerPosition => _mowerMover.CurrentPosition.Value;

        public bool IsRunning { get; set; } = false;
        
        private ILevelTraversalChecker _traversalChecker;

        private MowerMover _mowerMover;

        private IUndoSystem _undoManager;
        
        #region Unity

        private void OnEnable()
        {
            if (mowerInputEventChannel != null)
            {
                mowerInputEventChannel.EventRaised += OnInput;
            }
        }

        private void OnDisable()
        {
            if (mowerInputEventChannel != null)
            {
                mowerInputEventChannel.EventRaised -= OnInput;
            }
        }

        #endregion

        #region API

        public void Construct(MowerInputEventChannel mowerInputEventChannel)
        {
            this.mowerInputEventChannel = mowerInputEventChannel;
            
            OnEnable();
        }

        public void Init(MowerMover mowerMover, ILevelTraversalChecker traversalChecker, IUndoSystem undoManager)
        {
            _mowerMover = mowerMover;

            _traversalChecker = traversalChecker;

            _undoManager = undoManager;
        }

        public void Clear()
        {
            _mowerMover = null;

            _traversalChecker = null;

            _undoManager = null;
        }

        [Button(Expanded = true)]
        public void SetPosition(GridVector position)
        {
            _mowerMover.Move(position, true);
        }

        #endregion

        #region Events

        private void OnInput(GridVector direction)
        {
            if (_mowerMover == null)
            {
                Debug.LogWarning("Attempting to move a null mower");
                return;
            }
            
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