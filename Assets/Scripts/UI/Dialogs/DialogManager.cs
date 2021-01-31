using System;
using Core;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Core.EventChannels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Dialogs
{
	[UnreferencedScriptableObject]
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
			if (!openDialogEventChannel)
			{
				return;
			}
			SceneManager.activeSceneChanged += SceneManager_sceneLoaded;

			openDialogEventChannel.EventRaised += Show;
			closeDialogEventChannel.EventRaised += Close;
			
			FindDialogContainer();
		}

		private void OnDisable()
		{
			if (!openDialogEventChannel)
			{
				return;
			}
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
			if (!_idToDialogDict.ContainsKey(dialogId))
			{
				Debug.LogError($"Tried to close dialog with id {dialogId} but it doesn't exist");
				return;
			}

			Dialog dialog = _idToDialogDict[dialogId];
			dialog.Close();
		}

		private void OnDialogHidden(Dialog dialog)
		{
			DestroyDialog(dialog);
		}

		private void SceneManager_sceneLoaded(Scene sceneOut, Scene sceneIn)
		{
			FindDialogContainer();
		}

		#endregion

		#region Methods

		private void FindDialogContainer()
		{
			GameObject containerObj = GameObject.FindGameObjectWithTag(UnityTags.DialogCanvas);

			if (!containerObj)
			{
				return;
			}
			
			_dialogContainer = containerObj.transform;
		}

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
	}
}