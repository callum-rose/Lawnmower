using Core;
using Exceptions;
using Game.Cameras;
using Game.Levels;
using Game.Mowers;
using UI.Dialogs;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UI.Buttons;
using UnityEngine.Serialization;

namespace Game.Core
{
	internal class GameManager : BaseSceneManager
	{
		[SerializeField] private CameraManager cameraManager;
		[FormerlySerializedAs("mowerObjectCreator")] [FormerlySerializedAs("mowerCreator")] [SerializeField] private MowerManager mowerManager;
		[SerializeField] private LevelManager levelManager;
		[SerializeField, AssetsOnly] private LevelDataManager levelDataManager;
		[SerializeField] private IUndoSystemContainer undoSystemContainer;
		[SerializeField] private UndoController undoController;
		[SerializeField, AssetsOnly] private DialogManager dialogManager;

		private IUndoSystem UndoSystem => undoSystemContainer.Result;

		private GameSetupPassThroughData? _inputData;

		private GameObject _mowerObject;

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

			_mowerObject = mowerManager.Create(_inputData.Value.Mower, UndoSystem);

			levelManager.SetLevel(_inputData.Value.Level);

			cameraManager.Init(_mowerObject.transform);

			_levelDataRecorder = new LevelDataRecorder(UndoSystem);
			_levelDataRecorder.StartRecording();
		}

		public void End()
		{
			levelManager.ClearTiles();
			cameraManager.Clear();

			if (_mowerObject != null)
			{
				Destroy(_mowerObject.gameObject);
			}

			_inputData = null;

			UndoSystem.Reset();
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
				PersistantData.Level.SaveLevelMetaData(_inputData.Value.Level.Id, recordedData);
				
				int levelIndex = levelDataManager.GetLevelIndex(_inputData.Value.Level);
				levelDataManager.SetLevelCompleted(levelIndex);

				int nextLevelIndex = levelIndex + 1;
				if (levelDataManager.TryGetLevel(nextLevelIndex, out LevelData nextLevel))
				{
					MowerData cachedMower = _inputData.Value.Mower;

					void ButtonAction()
					{
						Begin(new GameSetupPassThroughData(cachedMower, nextLevel));
					}

					;

					_levelCompletedDialogId = dialogManager.Show("Level Completed!", "Nice one",
						new ButtonInfo("Next Level", action: ButtonAction));
				}
				else
				{
					// TODO
					throw new NotImplementedException();
				}
			}
			else
			{
				dialogManager.Close(_levelCompletedDialogId);
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