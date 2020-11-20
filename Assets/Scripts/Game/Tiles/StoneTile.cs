namespace Game.Tiles
{
    internal class StoneTile : Tile
    {
        public override bool IsTraversable(bool editMode) => false;
        public override bool IsComplete => true;
    }
}
