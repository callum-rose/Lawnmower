using Game.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Core
{
#if UNITY_EDITOR
	public partial class ViewManager
	{
		private const string EditorPrefTag = "EnteredPlayFromViewManager";

		private static bool WasAppStartedByViewManager => EditorPrefs.GetBool(EditorPrefTag, false);

		#region Load First

		// Shown and driven by ViewManagerEditor ("Enter Play Mode" / "Load Scene" buttons).
		[Header("Scene To Load")]
		[SerializeField] private UnityScene sceneToOpen;

		[SerializeField, Tooltip("Only used when Scene To Open is Game")]
		private GameSetupPassThroughData gameSetupPassThroughData;

		internal void EnterPlayModeFromEditor()
		{
			SetAppWasStartedByViewManager(true);

			EditorApplication.EnterPlaymode();
		}

		internal void LoadSelectedSceneFromEditor()
		{
			Load(sceneToOpen, GetData());
		}

		#endregion

		#region Methods

		private object GetData()
		{
			return sceneToOpen switch
			{
				UnityScene.Game => gameSetupPassThroughData,
				_ => null
			};
		}

		private static void SetAppWasStartedByViewManager(bool wasSet)
		{
			EditorPrefs.SetBool(EditorPrefTag, wasSet);
		}

		#endregion
	}
#endif
}
