using System;
using Game.Core;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core
{
#if UNITY_EDITOR
	public partial class ViewManager
	{
		private const string SceneToOpenTitle = "Scene To Load";

		private const string EditorPrefTag = "EnteredPlayFromViewManager";

		private static bool WasAppStartedByViewManager => EditorPrefs.GetBool(EditorPrefTag, false);
		
		#region Load First

		[TitleGroup(SceneToOpenTitle), SerializeField,
		 EnumToggleButtons, HideLabel]
		private UnityScene sceneToOpen;

		[TitleGroup(SceneToOpenTitle), SerializeField, ShowIf("@this.sceneToOpen == UnityScene.Game")]
		private GameSetupPassThroughData gameSetupPassThroughData;

		[TitleGroup(SceneToOpenTitle), Button, HideInPlayMode]
		private void EnterPlayMode()
		{
			SetAppWasStartedByViewManager(true);

			UnityEditor.EditorApplication.EnterPlaymode();
		}

		[TitleGroup(SceneToOpenTitle), Button("Load Scene"), ShowIf("@UnityEditor.EditorApplication.isPlaying")]
		private void Load_Odin()
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