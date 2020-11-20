using UnityEngine;

namespace Pool
{
    internal abstract class AutoShrinkPool<T> : MonoBehaviour, IPool<T> where T : MonoBehaviour
    {
        private MonoBehaviourPool<T> _pool;

        public void Init(T prefab, Transform container = null)
        {
            _pool = new MonoBehaviourPool<T>(prefab, container);
        }

        public void Empty()
        {
            _pool.Empty();
        }

        public void Enpool(T obj)
        {
            _pool.Enpool(obj);
        }

        public T Get()
        {
            return _pool.Get();
        }


    }
}
