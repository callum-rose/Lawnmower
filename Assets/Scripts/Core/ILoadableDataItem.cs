namespace Core
{
    internal interface ILoadableDataItem<T>
    {
        T Load();
    }
}
