namespace Core
{
    public interface ILoadableDataItem<T>
    {
        T Load();
    }
}
