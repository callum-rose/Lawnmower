using Lawnmower.Model;

namespace Game.Tiles
{
    public sealed class StoneTile : Tile, IReadonlyStoneTile
    {
        public override TileKind Kind => TileKind.Stone;
        public override bool IsTraversable => false;
        public override bool IsComplete => true;

        /// <summary>Visual rotation only (0-3).</summary>
        public int Direction { get; }

        public StoneTile(int direction = 0)
        {
            Direction = ((direction % 4) + 4) % 4;
        }

        public override TileSpec ToSpec() => TileSpec.Stone(Direction);
    }
}
