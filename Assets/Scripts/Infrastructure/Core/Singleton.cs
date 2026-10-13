using UnityEngine;
using UnityEngine.Assertions;

namespace Core
{
    public abstract class Singleton<T> : ScriptableObjectWithCoroutines where T : ScriptableObject
    {
        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                T[] instances = Resources.LoadAll<T>("");
                Assert.IsTrue(instances.Length == 1);
                _instance = instances[0];
                
                return _instance;
            }
        }

        private static T _instance;
    }
}
