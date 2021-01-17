using Core;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Core.EventChannels;
using UI.Buttons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Dialogs
{
    [CreateAssetMenu(fileName = nameof(DialogManager), menuName = SONames.GameDir + nameof(DialogManager))]
    internal class DialogManager : ScriptableObject
    {
        [SerializeField, AssetsOnly] private Dialog dialogPrefab;
        [SerializeField, AssetsOnly] private OpenDialogEventChannel openDialogEventChannel;
        [SerializeField, AssetsOnly] private CloseDialogEventChannel closeDialogEventChannel;

        private int _currentDialogId = 0;
        private readonly Dictionary<int, Dialog> _idToDialogDict = new Dictionary<int, Dialog>();

        private Transform _dialogContainer;

        #region Unity

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManager_sceneLoaded;
            
            openDialogEventChannel.EventRaised += Show;
            closeDialogEventChannel.EventRaised += Close;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManager_sceneLoaded;
            
            openDialogEventChannel.EventRaised -= Show;
            closeDialogEventChannel.EventRaised -= Close;
        }

        #endregion

        #region Events

        private int Show(DialogInfo info)
        {
            _currentDialogId++;
            
            Dialog dialog = Create();
            dialog.Init(_currentDialogId, info, () => Close(_currentDialogId));

            _idToDialogDict.Add(_currentDialogId, dialog);
            return _currentDialogId;
        }

        private void Close(int dialogId)
        {
            Dialog dialog = _idToDialogDict[dialogId];
            dialog.Close();
        }
        
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
