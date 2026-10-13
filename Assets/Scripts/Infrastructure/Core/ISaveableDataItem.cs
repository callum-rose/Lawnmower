namespace Core
{
    public interface ISaveableDataItem<T>
    {
        void Save(T value);
    }
}
