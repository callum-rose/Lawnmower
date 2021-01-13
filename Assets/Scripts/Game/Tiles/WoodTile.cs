using System;

namespace Game.Tiles
{
    [Serializable]
    internal class WoodTile : Tile
    {
        public override bool IsTraversable(bool editMode) => true;
        public override bool IsComplete => true;
    }
}