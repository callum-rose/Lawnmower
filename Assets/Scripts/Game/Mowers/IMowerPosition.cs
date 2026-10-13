using Game.Core;

namespace Game.Mowers
{
    public interface IMowerPosition
    {
        IListenableProperty<GridVector> CurrentPosition { get; }
    }
}