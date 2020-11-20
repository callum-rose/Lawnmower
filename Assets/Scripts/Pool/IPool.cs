namespace Pool
{
    internal interface IPool<T>
    {
        T Get();
        void Enpool(T obj);
        void Empty();
    }
}
