using Game.Core;
using Lawnmower.Model;

namespace Game.Tiles
{
	/// <summary>
	/// One of a pair of burrows. Entering it sends the mower out of the other hole of its pair and on one tile
	/// further in the direction it was going. If that tile can't be landed on, the move bounces (see
	/// <see cref="MovementKind.Burrow"/>). Never mowed.
	/// </summary>
	public sealed class RabbitHoleTile : Tile, IReadonlyRabbitHoleTile
	{
		public override TileKind Kind => TileKind.RabbitHole;
		public override bool IsTraversable => true;
		public override bool IsComplete => true;

		public int Pair { get; }

		/// <summary>The other hole of this pair, set by the level. Null if the pair has no other hole.</summary>
		public GridVector? Partner { get; internal set; }

		public RabbitHoleTile(int pair)
		{
			Pair = pair;
		}

		public override TileSpec ToSpec() => TileSpec.RabbitHole(Pair);

		public override TileRedirect? Redirect(GridVector position, GridVector arrivalDirection) =>
			Partner.HasValue ? new TileRedirect(Partner.Value + arrivalDirection, MovementKind.Burrow) : null;
	}
}
