using Game.Core;
using Lawnmower.Model;

namespace Game.Tiles
{
	public sealed class SpringTile : Tile, IReadonlySpringTile
	{
		public override TileKind Kind => TileKind.Spring;
		public override bool IsTraversable => true;
		public override bool IsComplete => true;

		/// <summary>Landing position relative to this tile.</summary>
		public GridVector Offset { get; }

		public SpringTile(GridVector offset)
		{
			Offset = offset;
		}

		public override TileSpec ToSpec() => TileSpec.Spring(Offset);

		public override TileRedirect? Redirect(GridVector position, GridVector arrivalDirection) =>
			new TileRedirect(position + Offset, MovementKind.Jump);
	}
}
