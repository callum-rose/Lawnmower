using Core;
using System.Collections;
using System.Collections.Generic;
using UI.Buttons;
using UI.Dialogs;
using UnityEngine;

namespace Game.UI
{
	internal class UIManager : MonoBehaviour
	{
		[SerializeField] private ButtonResizer quitButtonResizer, undoButtonResizer;
        [SerializeField] private float buttonShrinkPause = 3;
        [SerializeField] private DialogManager dialogManager;
      
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
            dialogManager.Show(
                "Quit Level",
                "Are you sure? You'll lose your progress",
                new ButtonInfo("Play On"),
                new ButtonInfo("Quit", action: () => ViewManager.Instance.Load(UnityScene.LevelSelect)));
        }

        #endregion
    }
}