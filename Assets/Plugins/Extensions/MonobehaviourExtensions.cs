using UnityEngine;

namespace BalsamicBits.Extensions
{
    public static class MonobehaviourExtensions
    {
        public static T GetOrAddComponent<T>(this MonoBehaviour mono) where T : Component
        {
            T component = mono.GetComponent<T>();
            if (component == null)
                component = mono.gameObject.AddComponent<T>();
            return component;
        }

        public static void StopCoroutineChecked(this MonoBehaviour mono, Coroutine coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            mono.StopCoroutine(coroutine);
        }
    }
}