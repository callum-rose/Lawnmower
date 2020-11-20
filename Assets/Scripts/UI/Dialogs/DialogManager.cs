using Core;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UI.Buttons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Dialogs
{
    [CreateAssetMenu(fileName = nameof(DialogManager), menuName = SONames.GameDir + nameof(DialogManager))]
    internal class DialogManager : ScriptableObject
    {
        [SerializeField, AssetsOnly] private Dialog dialogPrefab;

        private static int _currentDialogId = 0;
        private readonly static Dictionary<int, Dialog> _idToDialogDict = new Dictionary<int, Dialog>();

        private Transform _dialogContainer;

        #region Unity

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManager_sceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManager_sceneLoaded;
        }

        #endregion

        #region API

        /// <summary>
        /// Return dialog ID
        /// </summary>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <param name="buttonInfo"></param>
        /// <returns>Dialog ID</returns>
        public int Show(string header, string body, ButtonInfo buttonInfo)
        {
            _currentDialogId++;
            
            Dialog dialog = Create();
            dialog.Init(
                _currentDialogId,
                header, body,
                new ButtonInfo(
                    message: buttonInfo.Message,
                    action: () =>
                    {
                        buttonInfo.Action();
                        dialog.Close();
                    }));

            _idToDialogDict.Add(_currentDialogId, dialog);
            return _currentDialogId;
        }

        public int Show(string header, string body, ButtonInfo cancelButtonInfo, ButtonInfo acceptButtonInfo)
        {
            _currentDialogId++;

            Dialog dialog = Create();
            dialog.Init(
                _currentDialogId,
                header, body,
                new ButtonInfo(
                    message: cancelButtonInfo.Message, 
                    action: () =>
                    {
                        cancelButtonInfo.Action?.Invoke();
                        dialog.Close();
                    }),
                new ButtonInfo(
                    message: acceptButtonInfo.Message, 
                    action: () =>
                    {
                        acceptButtonInfo.Action?.Invoke();
                        dialog.Close();
                    }));

            dialog.Show();

            _idToDialogDict.Add(_currentDialogId, dialog);
            return _currentDialogId;
        }

        public void Close(int dialogId)
        {
            Dialog dialog = _idToDialogDict[dialogId];
            dialog.Close();
        }

        #endregion

        #region Events

        private void OnDialogHidden(Dialog dialog)
        {
            DestroyDialog(dialog);
        }

        private void SceneManager_sceneLoaded(Scene sceneOut, Scene sceneIn)
        {
            GameObject containerObj =  GameObject.FindGameObjectWithTag(UnityTags.DialogCanvas);
            _dialogContainer = containerObj.transform;
        }

        #endregion

        #region Methods

        private Dialog Create()
        {
            Dialog dialog = Instantiate(dialogPrefab, _dialogContainer);
            dialog.Hidden += OnDialogHidden;
            return dialog;
        }

        private void DestroyDialog(Dialog dialog)
        {
            _idToDialogDict.Remove(dialog.Id);
            Destroy(dialog.gameObject);
        }

        #endregion

        //private struct DialogInfo
        //{
        //    public DialogInfo(string header, string body, ButtonInfo buttonInfo) : this()
        //    {
        //        Header = header;
        //        Body = body;
        //        ButtonInfo = buttonInfo;
        //    }

        //    public string Header { get; }
        //    public string Body { get; }
        //    public ButtonInfo ButtonInfo { get; }
        //}
    }
}
