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

		[Header("Scene To Load")]
		[SerializeField] private UnityScene sceneToOpen;

		[SerializeField] private GameSetupPassThroughData gameSetupPassThroughData;

		[ContextMenu("Enter Play Mode")]
		private void EnterPlayMode()
		{
			SetAppWasStartedByViewManager(true);

			EditorApplication.EnterPlaymode();
		}

		[ContextMenu("Load Scene")]
		private void LoadSelectedScene()
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
