namespace Core
{
    internal interface ISaveableDataItem<T>
    {
        void Save(T value);
    }
}
