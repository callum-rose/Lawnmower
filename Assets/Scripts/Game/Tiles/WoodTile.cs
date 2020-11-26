namespace Game.Tiles
{
    internal class WoodTile : Tile
    {
        public override bool IsTraversable(bool editMode) => true;
        public override bool IsComplete => true;
    }
}