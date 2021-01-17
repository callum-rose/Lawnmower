using Core;
using System.Collections;
using Core.EventChannels;
using Sirenix.OdinInspector;
using UI.Buttons;
using UI.Dialogs;
using UnityEngine;

namespace Game.UI
{
    internal class UIManager : MonoBehaviour
	{
		[SerializeField] private ButtonResizer quitButtonResizer, undoButtonResizer;
        [SerializeField] private float buttonShrinkPause = 3;

        [TitleGroup("Event Channels")]
        [SerializeField] private OpenDialogEventChannel openDialogEventChannel;
        
        #region Unity

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(buttonShrinkPause);

            quitButtonResizer.SetWidth(200);
            undoButtonResizer.SetWidth(200);
        }

        #endregion

        #region Unity Button Events

        public void OnQuitButton()
        {
            DialogInfo dialogInfo = new DialogInfo(
                "Quit Level",
                "Are you sure? You'll lose your progress",
                new ButtonInfo("Play On"),
                new ButtonInfo("Quit", action: () => ViewManager.Instance.Load(UnityScene.LevelSelect)));

            openDialogEventChannel.Raise(dialogInfo);
        }

        #endregion
    }
}