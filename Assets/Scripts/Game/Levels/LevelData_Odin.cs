#if UNITY_EDITOR
using System;
using Sirenix.OdinInspector;
using UnityEditor;

namespace Game.Levels
{
	internal partial class LevelData
	{
		/// <summary>Set by the editor assembly so the inspector button can open the level editor window.</summary>
		internal static Action<LevelData> OpenInLevelEditor;

		[Button]
		private void SelectInProjectWindow()
		{
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = this;
		}

		[Button]
		private void OpenLevelEditorWindow()
		{
			OpenInLevelEditor?.Invoke(this);
		}
	}
}
#endif
