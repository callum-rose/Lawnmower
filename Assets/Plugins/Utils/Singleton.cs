using System;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Inherit from this base class to create a singleton.
    /// e.g. public class MyClassName : Singleton<MyClassName> {}
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// Access singleton instance through this propriety.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_shuttingDown)
                {
                    //Debug.LogWarning("[Singleton] Instance '" + typeof(T) + "' already destroyed. Returning null.");
                    return null;
                }

                //if (_instance == null)
                //{
                //    LogUtil.WriteWarning($"Singleton of type {typeof(T)} has no instance set. Performing lazy initialisation. Please use Init() to initialise properly");
                //    InitSingleton();
                //}

                return _instance;
            }
        }

        private static T _instance;

        // Check to see if we're about to be destroyed.
        private static bool _shuttingDown = false;
        private static object _lock = new object();

        public static void InitSingleton()
        {
            lock (_lock)
            {
                // Search for existing instance.
                _instance = (T)FindObjectOfType(typeof(T));

                if (_instance == null)
                {
                    throw new NullReferenceException($"Singleton instance of type {typeof(T)} not found");
                }

                //// Create new instance if one doesn't already exist.
                //if (_instance == null)
                //{
                //    // Need to create a new GameObject to attach the singleton to.
                //    GameObject singletonObject = new GameObject();
                //    _instance = singletonObject.AddComponent<T>();
                //    singletonObject.name = typeof(T).ToString() + " (Singleton)";
                //}

                // Make instance persistent.
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
#endif
                    // set root as dont destroy to stop those warnings
                    DontDestroyOnLoad(_instance.transform.root.gameObject);
#if UNITY_EDITOR
                }
#endif

                //LogUtil.Write($"Initialised singleton of type {typeof(T)}");
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _shuttingDown = true;
        }

        protected virtual void OnDestroy()
        {
            if (this == _instance)
            {
                _instance = null;
            }
        }
    }
}