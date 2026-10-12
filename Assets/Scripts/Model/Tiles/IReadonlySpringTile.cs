using Game.Core;

namespace Game.Tiles
{
	public interface IReadonlySpringTile : IReadonlyTile
	{
		GridVector Offset { get; }
	}
}