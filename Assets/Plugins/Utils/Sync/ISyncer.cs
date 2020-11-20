using System;

namespace Utils.Sync
{
    public interface ISyncer
    {
        void Sync(Action action);
    }
}