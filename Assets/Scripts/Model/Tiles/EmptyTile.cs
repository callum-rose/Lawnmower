using Lawnmower.Model;

namespace Game.Tiles
{
    public sealed class EmptyTile : Tile
    {
        public override TileKind Kind => TileKind.Empty;
        public override bool IsTraversable => false;
        public override bool IsComplete => true;

        public override TileSpec ToSpec() => TileSpec.Empty;
    }
}
