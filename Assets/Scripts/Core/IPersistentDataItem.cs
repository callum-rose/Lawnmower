namespace Core
{
    internal interface IPersistentDataItem<T> : ILoadableDataItem<T>, ISaveableDataItem<T>
    {

    }

    internal static class PersistentDataExtensions
    {
        public static ILoadableDataItem<T> GetLoader<T>(this ILoadableDataItem<T> loadable)
        {
            return loadable;
        }
    }
}
