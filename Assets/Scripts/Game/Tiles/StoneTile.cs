using System;

namespace Game.Tiles
{
    [Serializable]
    internal class StoneTile : Tilee
    {
        public override bool IsTraversable(bool editMode) => false;
        public override bool IsComplete => true;
    }
}
