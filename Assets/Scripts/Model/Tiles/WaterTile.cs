using Lawnmower.Model;

namespace Game.Tiles
{
    public sealed class WaterTile : Tile
    {
        public override TileKind Kind => TileKind.Water;
        public override bool IsTraversable => false;
        public override bool IsComplete => true;

        public override TileSpec ToSpec() => TileSpec.Water;
    }
}
