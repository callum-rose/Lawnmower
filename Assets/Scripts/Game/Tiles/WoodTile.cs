namespace Game.Tiles
{
    internal class WoodTile : Tile
    {
        public override bool IsTraversable(bool editMode) => false;
        public override bool IsComplete => true;
    }
}
