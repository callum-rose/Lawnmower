using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Plugins.Utils
{
	[CreateAssetMenu(fileName = nameof(ScriptableObjectReferencer),
		menuName = "Utils/" + nameof(ScriptableObjectReferencer))]
	public class ScriptableObjectReferencer : ScriptableObject
	{
#if UNITY_EDITOR
		[InfoBox("This objects should be stored in a resources folder to reference all scriptable objects that will be destroyed by the build",
			VisibleIf = nameof(IsInResourcesFolder))]
		[InfoBox("This objects should be stored in a resources folder to reference all scriptable objects that will be destroyed by the build",
			VisibleIf = nameof(IsNotInResourcesFolder), InfoMessageType = InfoMessageType.Error)]
#endif
		[SerializeField] private ScriptableObject[] references;

#if UNITY_EDITOR
		private bool IsInResourcesFolder => AssetDatabase.GetAssetPath(this).Contains("Resources");
		private bool IsNotInResourcesFolder => !IsInResourcesFolder;

		private void OnEnable()
		{
			if (IsInResourcesFolder)
			{
				return;
			}

			string path = AssetDatabase.GetAssetPath(this);
			Debug.LogError($"{nameof(ScriptableObjectReferencer)} at {path} needs to be places in a Resources folder");
		}
#endif
	}
}