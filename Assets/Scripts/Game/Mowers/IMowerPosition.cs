using Game.Core;

namespace Game.Mowers
{
    internal interface IMowerPosition
    {
        IListenableProperty<GridVector> CurrentPosition { get; }
    }
}