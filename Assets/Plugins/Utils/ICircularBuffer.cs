using System.Collections.Generic;

namespace Utils
{
    public interface ICircularBuffer<T> : IEnumerable<T>
    {
        int Count { get; }
        int Capacity { get; set; }
        T Enqueue(T item);
        T Dequeue();
        void Clear();
        T this[int index] { get; set; }
        int IndexOf(T item);
        void Insert(int index, T item);
        void RemoveAt(int index);
    }
}
