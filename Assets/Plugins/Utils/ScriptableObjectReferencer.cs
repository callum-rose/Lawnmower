using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Plugins.Utils
{
	[CreateAssetMenu(fileName = nameof(ScriptableObjectReferencer),
		menuName = "Utils/" + nameof(ScriptableObjectReferencer))]
	public class ScriptableObjectReferencer : ScriptableObject
	{
#if UNITY_EDITOR
		[Tooltip("This object should be stored in a Resources folder so it references all scriptable objects that would otherwise be stripped from the build")]
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