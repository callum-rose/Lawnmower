using Game.Core;

namespace Lawnmower.Model
{
	public enum MovementKind
	{
		/// <summary>One-tile move driven by input.</summary>
		Step,
		/// <summary>Launched by a tile (springs).</summary>
		Jump
	}

	/// <summary>Where a tile sends the mower after it lands there.</summary>
	public readonly struct TileRedirect
	{
		public GridVector Target { get; }
		public MovementKind Kind { get; }

		public TileRedirect(GridVector target, MovementKind kind)
		{
			Target = target;
			Kind = kind;
		}
	}
}
