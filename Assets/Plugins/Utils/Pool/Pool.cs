using System;
using System.Collections.Generic;

namespace Utils.Pool
{
    public class Pool<T>
    {
        private Stack<T> _stack;

        private readonly Func<T> _initialiser;

        public Pool(Func<T> initialiser, int preLoadSize = 0)
        {
            _stack = new Stack<T>(preLoadSize);

            _initialiser = initialiser;

            for (int i = 0; i < preLoadSize; i++)
            {
                _stack.Push(CreateNewItem());
            }
        }

        public T Get()
        {
            T item;
            if (_stack.Count == 0)
            {
                item = CreateNewItem();
            }
            else
            {
                item = _stack.Pop();
            }

            return item;
        }

        public void Put(T item)
        {
            _stack.Push(item);
        }

        private T CreateNewItem()
        {
            T newItem = _initialiser();
            return newItem;
        }
    }
}
