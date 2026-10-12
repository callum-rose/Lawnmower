using Lawnmower.Model;

namespace Game.Tiles
{
	public interface IReadonlyTile
	{
		TileKind Kind { get; }

		event TraverseEvent TraversedOnto;
		event TraverseEvent TraversedAway;
		event TraverseEvent BumpedInto;
	}
}
