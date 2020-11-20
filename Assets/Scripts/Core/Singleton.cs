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
                if (___instance == null)
                {
                    T[] instances = Resources.FindObjectsOfTypeAll<T>();
                    Assert.IsTrue(instances.Length == 1);
                    ___instance = instances[0];
                }

                return ___instance;
            }
        }
    }
}
