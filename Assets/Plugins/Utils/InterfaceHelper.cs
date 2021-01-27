using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace Utils
{
	public static class InterfaceHelper
	{
		private static Dictionary<Type, List<Type>> _interfaceToComponentMapping;
		private static Dictionary<Type, List<Type>> _interfaceToScriptableObjectMapping;
		private static Type[] _allTypes;

		private static readonly string[] _typesToSkip =
		{
			"unity", "system.", "mono.", "icsharpcode.", "nsubstitute", "nunit.", "microsoft.", "boo.",
			"serializ", "json", "log.", "logging", "test", "debug"
		};

		static InterfaceHelper()
		{
			InitInterfaceToComponentMapping();
		}

		private static void InitInterfaceToComponentMapping()
		{
			_interfaceToComponentMapping = new Dictionary<Type, List<Type>>();
			_interfaceToScriptableObjectMapping = new Dictionary<Type, List<Type>>();

			_allTypes = GetAllTypes();

			foreach (Type curInterface in _allTypes)
			{
				//We're interested only in interfaces
				if (!curInterface.IsInterface)
				{
					continue;
				}
				
				string typeName = curInterface.ToString().ToLower();
				
				//Skip system interfaces
				if (_typesToSkip.Any(s => typeName.Contains(s)))
				{
					continue;
				}

				IList<Type> typesInherited = GetComponentsAndSOsImplementingInterface(curInterface);

				if (typesInherited.Count <= 0)
				{
					continue;
				}

				HashSet<Type> componentsList = new HashSet<Type>();
				HashSet<Type> scriptableObjectList = new HashSet<Type>();

				foreach (Type curType in typesInherited)
				{
					//Skip interfaces
					if (curType.IsInterface)
					{
						continue;
					}

					if (typeof(Component).IsAssignableFrom(curType))
					{
						componentsList.Add(curType);
					}
					else if (typeof(ScriptableObject).IsAssignableFrom(curType))
					{
						scriptableObjectList.Add(curType);
					}
				}

				_interfaceToComponentMapping.Add(curInterface, componentsList.ToList());
				_interfaceToScriptableObjectMapping.Add(curInterface, scriptableObjectList.ToList());
			}
		}

		private static Type[] GetAllTypes()
		{
			List<Type> res = new List<Type>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				res.AddRange(assembly.GetTypes());
			}

			return res.ToArray();
		}

		public static IList<Type> GetComponentsAndSOsImplementingInterface(Type type)
		{
			//Caching
			_allTypes ??= GetAllTypes();

			return _allTypes
				.Where(curType => type.IsAssignableFrom(curType) &&
				                  (typeof(Component).IsAssignableFrom(curType) ||
				                   typeof(ScriptableObject).IsAssignableFrom(curType)))
				.ToList();
		}

		public static IList<Type> GetComponentsAndSOsImplementingInterface<T>() where T : class
		{
			return GetComponentsAndSOsImplementingInterface(typeof(T));
		}

		public static T[] FindObjects<T>(bool firstOnly = false) where T : class
		{
			List<T> foundObjects = new List<T>();

			foundObjects.AddRange(FindComponents<T>(firstOnly));
			foundObjects.AddRange(FindScriptableObjects<T>(firstOnly));

			if (foundObjects.Count == 0)
			{
				Debug.LogError("No descendants found for type " + typeof(T));
				return null;
			}

			return foundObjects.ToArray();
		}

		private static IList<T> FindComponents<T>(bool firstOnly = false) where T : class
		{
			List<T> resList = new List<T>();

			List<Type> types = null;
			if (_interfaceToComponentMapping.ContainsKey(typeof(T)))
			{
				types = _interfaceToComponentMapping[typeof(T)];
			}
			else
			{
				return new List<T>();
			}

			foreach (Type curType in types)
			{
				Object[] objects = firstOnly
					? new[] { Object.FindObjectOfType(curType) }
					: Object.FindObjectsOfType(curType);

				if (null == objects || objects.Length <= 0)
				{
					continue;
				}

				List<T> tList = new List<T>();

				foreach (Object curObj in objects)
				{
					T curObjAsT = curObj as T;

					if (null == curObjAsT)
					{
						Debug.LogError("Unable to cast '" + curObj.GetType() + "' to '" + typeof(T) + "'");
						continue;
					}

					tList.Add(curObjAsT);
				}

				resList.AddRange(tList);
			}

			return resList;
		}

		private static IList<T> FindScriptableObjects<T>(bool firstOnly = false) where T : class
		{
			List<T> resList = new List<T>();

			List<Type> types = null;
			if (_interfaceToScriptableObjectMapping.ContainsKey(typeof(T)))
			{
				types = _interfaceToScriptableObjectMapping[typeof(T)];
			}
			else
			{
				return new List<T>();
			}

			foreach (Type curType in types)
			{
#if UNITY_EDITOR
				string[] paths = AssetDatabase.FindAssets("t:" + curType.Name).Select(AssetDatabase.GUIDToAssetPath)
					.ToArray();
				if (paths.Length == 0)
				{
					foreach (string path in paths)
					{
						if (!path.Contains("Resources"))
						{
							Debug.LogError("Found asset of type " + curType + " at " + path +
							               ". This needs to be moved to a Resources folder to be found in a build");
						}
					}
				}
#endif

				Object[] objects = Resources.LoadAll(string.Empty, curType);
				resList.AddRange(objects.OfType<T>());
			}

			return resList;
		}

		public static T FindObject<T>() where T : class
		{
			IList<T> list = FindObjects<T>();

			if (list.Count == 0)
			{
				return default;
			}
			
			return list[0];
		}

		public static IList<T> GetInterfaceComponents<T>(this Component component, bool firstOnly = false)
			where T : class
		{
			List<Type> types = _interfaceToComponentMapping[typeof(T)];

			if (null == types || types.Count <= 0)
			{
				Debug.LogError("No descendants found for type " + typeof(T));
				return null;
			}

			List<T> resList = new List<T>();

			foreach (Type curType in types)
			{
				//Optimization - don't get all objects if we need only one
				Component[] components = firstOnly
					? new[] { component.GetComponent(curType) }
					: component.GetComponents(curType);

				if (null == components || components.Length <= 0)
					continue;

				List<T> tList = new List<T>();

				foreach (Component curComp in components)
				{
					T curCompAsT = curComp as T;

					if (null == curCompAsT)
					{
						Debug.LogError("Unable to cast '" + curComp.GetType() + "' to '" + typeof(T) + "'");
						continue;
					}

					tList.Add(curCompAsT);
				}

				resList.AddRange(tList);
			}

			return resList;
		}

		public static T GetInterfaceComponent<T>(this Component component) where T : class
		{
			IList<T> list = GetInterfaceComponents<T>(component, true);

			return list[0];
		}
	}
}