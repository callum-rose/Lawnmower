using System;

namespace Game.Tiles
{
    [Serializable]
    internal class WaterTile : Tilee
    {
        public override bool IsTraversable(bool editMode) => false;
        public override bool IsComplete => true;
    }
}
