using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace Game
{
    public static class InterfaceHelper
    {
        private static Dictionary<Type, List<Type>> _interfaceToComponentMapping;
        private static Type[] _allTypes;

        static InterfaceHelper()
        {
            InitInterfaceToComponentMapping();
        }

        private static void InitInterfaceToComponentMapping()
        {
            _interfaceToComponentMapping = new Dictionary<Type, List<Type>>();

            _allTypes = GetAllTypes();

            foreach (Type curInterface in _allTypes)
            {
                //We're interested only in interfaces
                if (!curInterface.IsInterface)
                    continue;

                string typeName = curInterface.ToString().ToLower();

                //Skip system interfaces
                if (typeName.Contains("unity") || typeName.Contains("system.")
                     || typeName.Contains("mono.") || typeName.Contains("mono.") || typeName.Contains("icsharpcode.")
                     || typeName.Contains("nsubstitute") || typeName.Contains("nunit.") || typeName.Contains("microsoft.")
                     || typeName.Contains("boo.") || typeName.Contains("serializ") || typeName.Contains("json")
                     || typeName.Contains("log.") || typeName.Contains("logging") || typeName.Contains("test")
                     || typeName.Contains("editor") || typeName.Contains("debug"))
                    continue;

                IList<Type> typesInherited = GetTypesInheritedFromInterface(curInterface);

                if (typesInherited.Count <= 0)
                    continue;

                List<Type> componentsList = new List<Type>();

                foreach (Type curType in typesInherited)
                {
                    //Skip interfaces
                    if (curType.IsInterface)
                        continue;

                    //Ignore non-component classes
                    if (!(typeof(Component) == curType || curType.IsSubclassOf(typeof(Component))))
                        continue;

                    if (!componentsList.Contains(curType))
                        componentsList.Add(curType);
                }

                _interfaceToComponentMapping.Add(curInterface, componentsList);
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

        private static IEnumerable<Type> GetTypesInheritedFromInterface<T>() where T : class
        {
            return GetTypesInheritedFromInterface(typeof(T));
        }

        private static IList<Type> GetTypesInheritedFromInterface(Type type)
        {
            //Caching
            if (null == _allTypes)
            {
                _allTypes = GetAllTypes();
            }

            List<Type> res = new List<Type>();

            foreach (Type curType in _allTypes)
            {
                if (!(type.IsAssignableFrom(curType) && curType.IsSubclassOf(typeof(Component))))
                    continue;

                res.Add(curType);

            }

            return res;
        }

        public static IList<T> FindObjects<T>(bool firstOnly = false) where T : class
        {
            List<T> resList = new List<T>();

            List<Type> types = _interfaceToComponentMapping[typeof(T)];

            if (null == types || types.Count == 0)
            {
                Debug.LogError("No descendants found for type " + typeof(T));
                return null;
            }

            foreach (Type curType in types)
            {
                Object[] objects = firstOnly ? new[] { Object.FindObjectOfType(curType) } : Object.FindObjectsOfType(curType);

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

        public static T FindObject<T>() where T : class
        {
            IList<T> list = FindObjects<T>();

            return list[0];
        }

        public static IList<T> GetInterfaceComponents<T>(this Component component, bool firstOnly = false) where T : class
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
                Component[] components = firstOnly ?
                    new[] { component.GetComponent(curType) }
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
