using Core;
using Exceptions;
using Game.Levels;
using Game.Mowers;
using Game.Mowers.Input;
using UI.Dialogs;
using Game.UndoSystem;
using BalsamicBits.Extensions;
using Core.EventChannels;
using Lawnmower.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UI.Buttons;
using UnityEngine.Serialization;

namespace Game.Core
{
	/// <summary>
	/// Owns one play session: builds the <see cref="LevelSimulation"/>, forwards input to it and maps
	/// its status changes onto the existing UI flow.
	/// </summary>
	internal class GameManager : BaseSceneManager
	{
		[FormerlySerializedAs("mowerObjectCreator"), FormerlySerializedAs("mowerCreator"), SerializeField]
		private MowerManager mowerManager;

		[SerializeField] private LevelManager levelManager;
		[SerializeField] private LevelDataManager levelDataManager;
		[SerializeField] private UndoController undoController;
		[SerializeField] private GamePlayData gamePlayData;

		[Header("Event Channels")]
		[SerializeField] private OpenDialogEventChannel openDialogEventChannel;
		[SerializeField] private CloseDialogEventChannel closeDialogEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer startPlayingEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer stopPlayingEventChannel;
		[SerializeField] private MowerInputEventChannel mowerInputEventChannel;
		[SerializeField] private UndoableEventChannel levelFailedChannel;

		private IVoidEventChannelTransmitter StartPlayingEventChannel => startPlayingEventChannel.Result;
		private IVoidEventChannelTransmitter StopPlayingEventChannel => stopPlayingEventChannel.Result;

		private readonly IUndoSystem _undoSystem = new UndoSystem.UndoSystem();

		private GameSetupPassThroughData? _inputData;
		private LevelSimulation _simulation;
		private MowerPositionAdapter _mowerPosition;
		private LevelDataRecorder _levelDataRecorder;
		private Coroutine _startPlayingTimer;
		private bool _acceptingInput;

		private int _levelCompletedDialogId;

		/// <summary>The running simulation, or null between levels.</summary>
		public LevelSimulation Simulation => _simulation;

		#region Unity

		private void Awake()
		{
			mowerInputEventChannel.EventRaised += OnMowerInput;
		}

		private void OnDestroy()
		{
			mowerInputEventChannel.EventRaised -= OnMowerInput;

			if (_simulation != null)
			{
				_simulation.StatusChanged -= OnStatusChanged;
				_simulation = null;
			}

			_levelDataRecorder?.Dispose();
		}

		#endregion

		#region API

		public override void Begin(object data)
		{
			End();

			if (data is GameSetupPassThroughData input)
			{
				_inputData = input;
			}
			else
			{
				throw new InvalidTypeException(data, typeof(GameSetupPassThroughData));
			}

			_simulation = new LevelSimulation(input.Level.CreateLevel(), _undoSystem);
			_simulation.StatusChanged += OnStatusChanged;

			undoController.Bind(_undoSystem);
			undoController.IsRunning = true;

			_mowerPosition = new MowerPositionAdapter(_simulation);
			mowerManager.Init(input.Mower, _mowerPosition);
			_mowerPosition.PublishInitial();

			levelManager.Init(_simulation.Level);

			_levelDataRecorder = new LevelDataRecorder(_undoSystem);
			_levelDataRecorder.StartRecording();

			_startPlayingTimer = this.Timer(gamePlayData.LevelIntroDuration, OnIntroFinished);
		}

		public void End()
		{
			_acceptingInput = false;

			if (_startPlayingTimer != null)
			{
				StopCoroutine(_startPlayingTimer);
				_startPlayingTimer = null;
			}

			if (_simulation != null)
			{
				_simulation.StatusChanged -= OnStatusChanged;
				_simulation = null;
			}

			_levelDataRecorder?.Dispose();
			_levelDataRecorder = null;

			if (levelManager)
			{
				levelManager.Clear();
			}

			if (mowerManager)
			{
				mowerManager.Clear();
			}

			_mowerPosition?.Dispose();
			_mowerPosition = null;

			_inputData = null;

			_undoSystem.Reset();

			if (undoController)
			{
				undoController.IsRunning = false;
			}

			StopPlayingEventChannel?.Raise();
		}

		#endregion

		#region Events

		private void OnIntroFinished()
		{
			_startPlayingTimer = null;
			_acceptingInput = true;
			StartPlayingEventChannel.Raise();
		}

		private void OnMowerInput(GridVector direction)
		{
			if (!_acceptingInput || _simulation == null)
			{
				return;
			}

			_simulation.Move(direction);
		}

		private void OnStatusChanged(LevelStatus from, LevelStatus to, Xor isUndo)
		{
			if (to == LevelStatus.Completed && !isUndo)
			{
				OnLevelCompleted();
			}
			else if (from == LevelStatus.Completed && isUndo)
			{
				closeDialogEventChannel.Raise(_levelCompletedDialogId);
			}

			if (to == LevelStatus.Failed && !isUndo)
			{
				levelFailedChannel.Raise(false);
			}
			else if (from == LevelStatus.Failed && isUndo)
			{
				levelFailedChannel.Raise(true);
			}
		}

		private void OnLevelCompleted()
		{
			Assert.IsTrue(_inputData.HasValue);

			LevelData level = _inputData.Value.Level;

			_levelDataRecorder.StopRecording();
			var recordedData = _levelDataRecorder.ExtractData();
			PersistantData.LevelModule.SaveLevelMetaData(level.Id, recordedData);

			levelDataManager.SetLevelCompleted(level);

			if (levelDataManager.GetLevelAfter(level, out LevelInfo nextLevelInfo))
			{
				MowerData cachedMower = _inputData.Value.Mower;

				void ButtonAction()
				{
					GameSetupPassThroughData data = new GameSetupPassThroughData(cachedMower, nextLevelInfo.LevelData);
					ViewManager.Instance.Load(UnityScene.Game, data);
				}

				DialogInfo dialogInfo = new DialogInfo(
					"Level Completed!",
					"Nice one",
					new ButtonInfo("Next Level", action: ButtonAction));
				_levelCompletedDialogId = openDialogEventChannel.Raise(dialogInfo);
			}
			else
			{
				ViewManager.Instance.Load(UnityScene.LevelSelect);
			}
		}

		#endregion
	}
}
