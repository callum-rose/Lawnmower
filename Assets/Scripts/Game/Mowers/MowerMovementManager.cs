using Core;
using Core.EventChannels;
using Game.Core;
using Game.Levels;
using Game.Mowers.Input;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(MowerMovementManager),
		menuName = SoNames.GameDir + nameof(MowerMovementManager))]
	public partial class MowerMovementManager : ScriptableObject, IMowerRunnable
	{
		[SerializeField] private MowerInputEventChannel mowerInputEventChannel;
		[SerializeField] private VoidEventChannel startPlayingEventChannel;
		[SerializeField] private VoidEventChannel stopPlayingEventChannel;

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

		private bool _isPlaying;

		#region Unity

		private void OnEnable()
		{
			if (mowerInputEventChannel != null)
			{
				mowerInputEventChannel.EventRaised += OnInput;
			}

			if (startPlayingEventChannel != null)
			{
				startPlayingEventChannel.EventRaised += OnStartPlaying;
			}

			if (stopPlayingEventChannel != null)
			{
				stopPlayingEventChannel.EventRaised += OnStopPlaying;
			}
		}

		private void OnDisable()
		{
			if (mowerInputEventChannel != null)
			{
				mowerInputEventChannel.EventRaised -= OnInput;
			}

			if (startPlayingEventChannel != null)
			{
				startPlayingEventChannel.EventRaised -= OnStartPlaying;
			}

			if (stopPlayingEventChannel != null)
			{
				stopPlayingEventChannel.EventRaised -= OnStopPlaying;
			}
		}

		#endregion

		#region API

		public void Construct(MowerInputEventChannel mowerInputEventChannel, VoidEventChannel startPlayingEventChannel)
		{
			this.mowerInputEventChannel = mowerInputEventChannel;
			this.startPlayingEventChannel = startPlayingEventChannel;

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
		public void SetInitialPosition(GridVector position)
		{
			_mowerMover.Move(position, true);
		}

		#endregion

		#region Events

		private void OnStartPlaying()
		{
			_isPlaying = true;
		}

		private void OnStopPlaying()
		{
			_isPlaying = false;
		}

		private void OnInput(GridVector direction)
		{
			if (!_isPlaying)
			{
				Debug.LogWarning("Is not playing so won't move mower");
				return;
			}
			
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

			MoveToPosition(targetPosition);
		}

		public void MoveToPosition(GridVector targetPosition)
		{
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