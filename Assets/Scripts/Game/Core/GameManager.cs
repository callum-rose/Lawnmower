using Core;
using Exceptions;
using Game.Levels;
using Game.Mowers;
using UI.Dialogs;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using BalsamicBits.Extensions;
using Core.EventChannels;
using UnityEngine;
using UnityEngine.Assertions;
using UI.Buttons;
using UnityEngine.Serialization;

namespace Game.Core
{
	internal class GameManager : BaseSceneManager
	{
		[FormerlySerializedAs("mowerObjectCreator"), FormerlySerializedAs("mowerCreator"), SerializeField]
		private MowerManager mowerManager;

		[SerializeField] private LevelManager levelManager;
		[SerializeField] private LevelTraversalChecker levelTraversalChecker;
		[SerializeField, AssetsOnly] private LevelDataManager levelDataManager;
		[SerializeField] private IUndoSystemContainer undoSystemContainer;
		[SerializeField] private UndoController undoController;
		[SerializeField] private GamePlayData gamePlayData;

		[TitleGroup("Event Channels")] 
		[SerializeField] private OpenDialogEventChannel openDialogEventChannel;
		[SerializeField] private CloseDialogEventChannel closeDialogEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer startPlayingEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer stopPlayingEventChannel;

		private IVoidEventChannelTransmitter StartPlayingEventChannel => startPlayingEventChannel.Result;
		private IVoidEventChannelTransmitter StopPlayingEventChannel => stopPlayingEventChannel.Result;

		private IUndoSystem UndoSystem => undoSystemContainer.Result;

		private GameSetupPassThroughData? _inputData;

		private LevelDataRecorder _levelDataRecorder;

		private int _levelCompletedDialogId, _levelFailedDialogId;

		#region Unity

		private void Awake()
		{
			levelManager.LevelCompleted += OnLevelCompleted;
			levelManager.LevelFailed += OnLevelFailed;
		}

		private void OnDestroy()
		{
			levelManager.LevelCompleted -= OnLevelCompleted;
			levelManager.LevelFailed -= OnLevelFailed;
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

			undoController.IsRunning = true;

			mowerManager.Init(_inputData.Value.Mower, levelTraversalChecker, UndoSystem);

			levelManager.Init(_inputData.Value.Level);

			_levelDataRecorder = new LevelDataRecorder(UndoSystem);
			_levelDataRecorder.StartRecording();

			this.Timer(gamePlayData.LevelIntroDuration, StartPlayingEventChannel.Raise);
		}

		public void End()
		{
			levelManager.Clear();

			mowerManager.Clear();

			_inputData = null;

			UndoSystem.Reset();
			
			StopPlayingEventChannel.Raise();
		}

		#endregion

		#region Events

		private void OnLevelCompleted(Xor isUndo)
		{
			if (!isUndo)
			{
				Assert.IsTrue(_inputData.HasValue);

				_levelDataRecorder.StopRecording();
				var recordedData = _levelDataRecorder.ExtractData();
				PersistantData.LevelModule.SaveLevelMetaData(_inputData.Value.Level.Id, recordedData);

				levelDataManager.SetLevelCompleted(_inputData.Value.Level);

				if (levelDataManager.GetLevelAfter(_inputData.Value.Level, out LevelInfo nextLevelInfo))
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
			else
			{
				closeDialogEventChannel.Raise(_levelCompletedDialogId);
			}
		}

		private void OnLevelFailed(Xor isUndo)
		{
			//if (!isUndo)
			//{
			//    void ButtonAction()
			//    {
			//        Begin(_inputData);
			//    };
			//    _levelFailedDialogId = dialogManager.Show("Level Completed!", "Nice one", new ButtonInfo("Retry", action: ButtonAction));
			//}
			//else
			//{
			//    dialogManager.Close(_levelFailedDialogId);
			//}
		}

		#endregion
	}
}