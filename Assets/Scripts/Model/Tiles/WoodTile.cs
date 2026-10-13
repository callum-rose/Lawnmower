using Lawnmower.Model;

namespace Game.Tiles
{
    public sealed class WoodTile : Tile, IReadonlyWoodTile
    {
        public override TileKind Kind => TileKind.Wood;
        public override bool IsTraversable => true;
        public override bool IsComplete => true;

        public override TileSpec ToSpec() => TileSpec.Wood;
    }
}
