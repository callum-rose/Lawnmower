using Game.Core;

namespace Game.Tiles
{
	public interface IReadonlyGrassTile : IReadonlyTile
	{
		IListenableProperty<int> GrassHeight { get; }
	}
}