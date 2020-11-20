namespace Core
{
    internal interface ILoadableDataItem<T>
    {
        bool TryLoad(out T value);
    }
}
