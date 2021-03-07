using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BalsamicBits.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Core
{
	public class ScriptableObjectReferencer : MonoBehaviour
	{
		[SerializeField] private List<ScriptableObject> scriptableObjects;

#if UNITY_EDITOR

		[MenuItem("Callum/Update Scriptable Objects Referencers")]
		[InitializeOnLoadMethod]
		public static void UpdateAll()
		{
			ScriptableObjectReferencer[] all = Resources
				.LoadAll<UnityEngine.Object>("")
				.Where(o => o is GameObject g && g.GetComponent<ScriptableObjectReferencer>() != null)
				.Select(o => (o as GameObject)!.GetComponent<ScriptableObjectReferencer>())
				.ToArray();

			if (all.Length == 0)
			{
				Debug.LogError($"Need one {nameof(ScriptableObjectReferencer)} in the Resources folder");
				return;
			}

			if (all.Length > 1)
			{
				Debug.LogError(
					$"Need only one {nameof(ScriptableObjectReferencer)} in the Resources folder. Found {all.Length}");
				return;
			}

			AssetDatabase
				.FindAssets("t:" + nameof(GameObject))
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
				.First(g => g.GetComponent<ScriptableObjectReferencer>() != null)
				.GetComponent<ScriptableObjectReferencer>()
				.FindAllUnreferencedSOs();
			
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log("Updated " + nameof(ScriptableObjectReferencer));
		}

		[Button]
		private void FindAllUnreferencedSOs()
		{
			scriptableObjects = new List<ScriptableObject>();

			IEnumerable<Type> types = AttributeHelper.GetTypesWithAttribute<UnreferencedScriptableObjectAttribute>(Assembly.GetExecutingAssembly());

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