using System;

namespace Game.Tiles
{
    [Serializable]
    internal class StoneTile : Tile
    {
        public override bool IsTraversable(bool editMode) => false;
        public override bool IsComplete => true;
    }
}
