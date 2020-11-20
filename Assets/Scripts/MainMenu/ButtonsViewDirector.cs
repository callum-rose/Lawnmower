using Core;
using UnityEngine;

namespace MainMenu
{
    [CreateAssetMenu(fileName = nameof(ButtonsViewDirector), menuName = SONames.CoreDir + nameof(ButtonsViewDirector))]
    public class ButtonsViewDirector : ScriptableObject
    {
        public void GoToMainMenu()
        {
            ViewManager.Instance.Load(UnityScene.MainMenu);
        }

        public void GoToLevelSelect()
        {
            ViewManager.Instance.Load(UnityScene.LevelSelect);
        }
    }
}
