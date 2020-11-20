namespace Game.Tiles
{
    internal class WaterTile : Tile
    {
        public override bool IsTraversable(bool editMode) => false;
        public override bool IsComplete => true;
    }
}
