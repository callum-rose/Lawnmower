using Core;
using Exceptions;
using Game.Camera;
using Game.Levels;
using Game.Mowers;
using UI.Dialogs;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UI.Buttons;

namespace Game.Core
{
    internal class GameManager : BaseSceneManager
    {
        [SerializeField] private CameraManager cameraManager;
        [SerializeField] private MowerCreator mowerCreator;
        [SerializeField] private LevelManager levelManager;
        [SerializeField, AssetsOnly] private LevelDataManager levelDataManager;
        [SerializeField] private IUndoSystemContainer undoSystemContainer;
        [SerializeField] private UndoController undoController;
        [SerializeField, AssetsOnly] private DialogManager dialogManager;

        private IUndoSystem UndoSystem => undoSystemContainer.Result;

        private GameSetupPassThroughData _inputData;

        private MowerManager _mower;

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

        public override void Begin(PassThroughData data)
        {
            End();

            if (data is GameSetupPassThroughData @in)
            {
                _inputData = @in;
            }
            else
            {
                throw new InvalidTypeException(data, typeof(GameSetupPassThroughData));
            }

            undoController.IsRunning = true;

            _mower = mowerCreator.Create(_inputData.Mower, UndoSystem);

            levelManager.Init(_mower.Movement);
            levelManager.SetLevel(_inputData.Level);

            cameraManager.Init(_mower.transform);
        }

        public void End()
        {
            levelManager.ClearTiles();
            cameraManager.Clear();

            if (_mower != null)
            {
                Destroy(_mower.gameObject);
            }

            _inputData = null;
        }

        #endregion

        #region Events

        private void OnLevelCompleted(Xor isUndo)
        {
            if (!isUndo)
            {
                //undoController.IsRunning = false;

                Assert.IsNotNull(_inputData);
                int levelIndex = levelDataManager.GetLevelIndex(_inputData.Level);
                levelDataManager.SetLevelCompleted(levelIndex);

                if (levelDataManager.TryGetLevel(levelIndex + 1, out LevelData nextLevel))
                {
                    void ButtonAction()
                    {
                        Begin(new GameSetupPassThroughData { Level = nextLevel, Mower = _inputData.Mower });
                    };
                    _levelCompletedDialogId = dialogManager.Show("Level Completed!", "Nice one", new ButtonInfo("Next Level", action: ButtonAction));
                }
                else
                {
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
