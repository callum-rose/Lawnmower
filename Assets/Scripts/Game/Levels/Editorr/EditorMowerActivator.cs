using Game.Mowers;
using UI.Buttons;
using UnityEngine;
using Utils;

namespace Game.Levels.Editorr
{
    internal class EditorMowerActivator : MonoBehaviour
	{
		#region Button Events
		
		public void Toggle()
        {
			IMowerRunnable runner = InterfaceHelper.FindObject<IMowerRunnable>();

			if (runner == null)
            {
				Debug.LogWarning("No mower found");
				return;
            }

			runner.IsRunning = !runner.IsRunning;

			GetComponent<ButtonIconSetter>().Set(runner.IsRunning ? IconType.Exit : IconType.Play);
        }

		#endregion
	}
}