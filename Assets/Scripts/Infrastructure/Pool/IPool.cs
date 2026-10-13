namespace Pool
{
    public interface IPool<T>
    {
        T Get();
        void Enpool(T obj);
        void Empty();
    }
}
