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