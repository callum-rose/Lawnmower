namespace Core
{
    public interface IPersistentDataItem<T> : ILoadableDataItem<T>, ISaveableDataItem<T>
    {

    }

    public static class PersistentDataExtensions
    {
        public static ILoadableDataItem<T> GetLoader<T>(this ILoadableDataItem<T> loadable)
        {
            return loadable;
        }
    }
}
