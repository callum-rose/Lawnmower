using Game.Core;

namespace Game.Tiles
{
	public interface IReadonlyRabbitHoleTile : IReadonlyTile
	{
		int Pair { get; }
		GridVector? Partner { get; }
	}
}
