#if UNITY_EDITOR

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Core
{
	internal static class DummyInitScriptableObjects
	{
		//[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		public static void Init()
		{
			Dictionary<ScriptableObject, ScriptableObjectMessages> messages =
				new Dictionary<ScriptableObject, ScriptableObjectMessages>();
			
			Debug.Log("Init");
			string[] scriptableObjectIds =
				AssetDatabase.FindAssets("t:" + nameof(ScriptableObject), new[] { "Assets/ScriptableObjects" });
			foreach (string id in scriptableObjectIds)
			{
				string path = AssetDatabase.GUIDToAssetPath(id);
				ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

				if (obj == null)
				{
					Debug.LogError(path);
					continue;
				}
				
				messages.Add(obj, new ScriptableObjectMessages());
				
				MethodInfo[] methods = obj.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				foreach (MethodInfo method in methods)
				{
					if (method.Name == "Awake")
					{
						messages[obj].awake = method;
					}

					if (method.Name == "OnEnable")
					{
						messages[obj].onEnable = method;
					}
				}
			}

			foreach (KeyValuePair<ScriptableObject, ScriptableObjectMessages> kv in messages)
			{
				MethodInfo method = kv.Value.awake;
				if (method == null)
                {
					continue;
                }

				ScriptableObject obj = kv.Key;
				method.Invoke(obj, null);
			}

			return;
			
			foreach (KeyValuePair<ScriptableObject, ScriptableObjectMessages> kv in messages)
			{
				MethodInfo method = kv.Value.onEnable;
				if (method == null)
				{
					continue;
				}

				ScriptableObject obj = kv.Key;
				method.Invoke(obj, null);
			}
		}

		private class ScriptableObjectMessages
		{
			public MethodInfo awake, onEnable;
		}
	}
}

#endif