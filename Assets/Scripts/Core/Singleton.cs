using UnityEngine;
using UnityEngine.Assertions;

namespace Core
{
    public abstract class Singleton<T> : ScriptableObjectWithCoroutines where T : ScriptableObject
    {
        private static T ___instance;
        public static T Instance
        {
            get
            {
                if (___instance != null)
                {
                    return ___instance;
                }

                T[] instances = Resources.LoadAll<T>("");
                Assert.IsTrue(instances.Length == 1);
                ___instance = instances[0];
                
                return ___instance;
            }
        }
    }
}
