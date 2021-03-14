using Game.Core;

namespace Game.Tiles
{
	internal interface IReadonlyGrassTile : IReadonlyTile
	{
		IListenableProperty<int> GrassHeight { get; }
	}
}