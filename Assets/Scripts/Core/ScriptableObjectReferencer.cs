using System;
using System.Collections.Generic;
using System.Linq;
using Core.EventChannels;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Utils;

namespace Core
{
	public class ScriptableObjectReferencer : MonoBehaviour
	{
		[SerializeField] private List<ScriptableObject> scriptableObjects;
		
#if UNITY_EDITOR

		[MenuItem("Callum/Update Scriptable Objects Referencers")]
		public static void UpdateAll()
		{
			var all = Resources.LoadAll<ScriptableObjectReferencer>("");

			if (all.Length == 0)
			{
				Debug.LogError($"Need one {nameof(ScriptableObjectReferencer)} in the Resources folder");
				return;
			}
			
			if (all.Length > 1)
			{
				Debug.LogError($"Need only one {nameof(ScriptableObjectReferencer)} in the Resources folder. Found {all.Length}");
				return;
			}
			
			foreach (var sor in all)
			{
				sor.FindAllUnreferencedSOs();
			}
			
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		
		[Button]
		private void FindAllUnreferencedSOs()
		{
			scriptableObjects = new List<ScriptableObject>();
			
			IList<Type> types =
				InterfaceHelper.GetComponentsAndSOsImplementingInterface<IUnreferencedScriptableObject>();

			foreach (Type type in types)
			{
				if (type.IsAbstract)
				{
					continue;
				}
				
				if (!typeof(ScriptableObject).IsAssignableFrom(type))
				{
					continue;
				}

				IEnumerable<ScriptableObject> foundSOs = AssetDatabase
					.FindAssets($"t:{type.Name}", new[] { "Assets/ScriptableObjects" })
					.Select(AssetDatabase.GUIDToAssetPath)
					.Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>);
				
				scriptableObjects.AddRange(foundSOs);
			}
		}
#endif
	}
}