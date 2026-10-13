using Game.Core;
using Game.UndoSystem;
using Lawnmower.Model;

namespace Game.Tiles
{
	/// <summary>
	/// Runtime tile state. Views subscribe to the traverse events; only the simulation mutates
	/// (the mutators are internal to the model assembly).
	/// </summary>
	public abstract class Tile : IReadonlyTile
	{
		public abstract TileKind Kind { get; }

		public abstract bool IsTraversable { get; }

		public abstract bool IsComplete { get; }

		public virtual bool IsRuined => false;

		public event TraverseEvent TraversedOnto;
		public event TraverseEvent TraversedAway;
		public event TraverseEvent BumpedInto;

		/// <summary>The tile's current configuration (including mutable state such as grass height).</summary>
		public abstract TileSpec ToSpec();

		public Tile Clone() => TileFactory.Create(ToSpec());

		/// <summary>
		/// Play-mode movement hook: where the mower continues after arriving on this tile at
		/// <paramref name="position"/>, travelling in <paramref name="arrivalDirection"/>.
		/// Null means the mower comes to rest here. Only consulted while planning a forward move.
		/// </summary>
		public virtual TileRedirect? Redirect(GridVector position, GridVector arrivalDirection) => null;

		internal virtual void TraverseOnto(GridVector fromDirection, Xor inversion)
		{
			TraversedOnto?.Invoke(fromDirection, inversion);
		}

		internal virtual void TraverseAway(GridVector toDirection, Xor inversion)
		{
			TraversedAway?.Invoke(toDirection, inversion);
		}

		internal virtual void BumpInto(GridVector fromDirection, Xor inversion)
		{
			BumpedInto?.Invoke(fromDirection, inversion);
		}

		public override string ToString()
		{
			return ToSpec().ToString();
		}
	}
}
