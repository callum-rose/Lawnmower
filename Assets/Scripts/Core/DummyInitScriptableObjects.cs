using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core
{
	internal static class EditorDummyInitScriptableObjects
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void LoadSoReferencerPrefab()
		{
			Debug.Log("Hello");

			ScriptableObjectReferencer[] referencers = Resources.LoadAll<ScriptableObjectReferencer>("");
			
			if (referencers.Length == 0)
			{
				Debug.LogError($"No prefabs of type {nameof(ScriptableObjectReferencer)} in Resources");
			}
			else if (referencers.Length != 1)
			{
				Debug.LogError($"Too many prefabs of type {nameof(ScriptableObjectReferencer)} in Resources");
			}

			Object obj = Object.Instantiate(referencers[0]);
			Object.DontDestroyOnLoad(obj);
		}

#if UNITY_EDITOR

		//[RuntimeInitializeOnLoadMethod]
		public static void Init()
		{
			Dictionary<ScriptableObject, ScriptableObjectMessages> messages =
				new Dictionary<ScriptableObject, ScriptableObjectMessages>();

			IList<Type>
				types = new[]
				{
					typeof(ScriptableObject)
				}; //InterfaceHelper.GetComponentsAndSOsImplementingInterface(typeof(IInitialisableScriptableObject));

			List<string> scriptableObjectIds = new List<string>();
			foreach (Type type in types.Where(t => !t.IsAbstract))
			{
				scriptableObjectIds.AddRange(AssetDatabase.FindAssets("t:" + type.Name,
					new[] { "Assets/ScriptableObjects" }));
			}

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

				MethodInfo[] methods = obj.GetType()
					.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				foreach (MethodInfo method in methods)
				{
					switch (method.Name)
					{
						case "Awake":
							messages[obj].Awake = method;
							break;
						case "OnEnable":
							messages[obj].OnEnable = method;
							break;
						case "OnDisable":
							messages[obj].OnDisable = method;
							break;
						case "OnDestroy":
							messages[obj].OnDestroy = method;
							break;
					}
				}
			}

			void FireMessageForAll(Func<ScriptableObjectMessages, MethodInfo> methodGetter)
			{
				foreach (KeyValuePair<ScriptableObject, ScriptableObjectMessages> kv in messages)
				{
					MethodInfo method = methodGetter.Invoke(kv.Value);
					ScriptableObject obj = kv.Key;
					method?.Invoke(obj, null);
				}
			}

			FireMessageForAll(s => s.OnDisable);
			FireMessageForAll(s => s.OnDestroy);
			FireMessageForAll(s => s.Awake);
			FireMessageForAll(s => s.OnEnable);
		}

		private class ScriptableObjectMessages
		{
			public MethodInfo Awake;
			public MethodInfo OnEnable;
			public MethodInfo OnDisable;
			public MethodInfo OnDestroy;
		}

#endif
	}
}