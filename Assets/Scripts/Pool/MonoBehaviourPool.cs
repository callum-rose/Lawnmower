using System.Collections.Generic;
using UnityEngine;

namespace Pool
{
    internal class MonoBehaviourPool<T> : IPool<T> where T : MonoBehaviour
    {
        private T _prefab;
        private Transform _container;
        private Stack<T> _pool = new Stack<T>();

        public MonoBehaviourPool(T prefab, Transform container = null)
        {
            _prefab = prefab;
            _container = container;
        }

        public T Get()
        {
            T obj;
            if (_pool.Count == 0)
            {
                obj = Create();
            }
            else
            {
                obj = _pool.Pop();
            }

            obj.gameObject.SetActive(true);

            return obj;
        }

        public void Enpool(T obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_container);
            _pool.Push(obj);
        }

        public void Empty()
        {
            while(_pool.Count > 0)
            {
                var obj = _pool.Pop();
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }

        private T Create()
        {
            var newObj = UnityEngine.Object.Instantiate(_prefab, _container);
            return newObj;
        }
    }
}
