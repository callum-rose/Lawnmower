using System;
using System.Collections.Generic;
using System.Linq;
using Core.EventChannels;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Core
{
	public class ScriptableObjectReferencer : MonoBehaviour
	{
		[InfoBox("Scene field must be set", InfoMessageType.Error, VisibleIf = nameof(SceneIsNone))]
		[SerializeField]
		private UnityScene thisScene = UnityScene.None;

		[SerializeField] private List<UnreferencedScriptableObject> scriptableObjects;

		private bool SceneIsNone => thisScene == UnityScene.None;
		
		private void OnEnable()
		{
			thisScene = (UnityScene) gameObject.scene.buildIndex;
		}

#if UNITY_EDITOR
		[Button]
		private void ReferenceAllScriptableObjects()
		{
			scriptableObjects = AssetDatabase
				.FindAssets($"t:{nameof(UnreferencedScriptableObject)}", new[] { "Assets/ScriptableObjects" })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<UnreferencedScriptableObject>)
				.Where(uso => uso.EnableInAllScenes || uso.ScenesToBeEnabledIn.Contains(thisScene))
				.ToList();
		}
#endif
	}
}