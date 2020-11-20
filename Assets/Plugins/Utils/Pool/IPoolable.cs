namespace Utils.Pool
{
    public interface IPoolable
    {
        // Called to reset an object when it is pooled
        void Reset();
    }
}