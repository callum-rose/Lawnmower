using System;

namespace Game.Tiles
{
    [Serializable]
    internal class WoodTile : Tilee
    {
        public override bool IsTraversable(bool editMode) => true;
        public override bool IsComplete => true;
    }
}