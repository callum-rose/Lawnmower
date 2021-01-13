using System;

namespace Game.Tiles
{
    [Serializable]
    internal class EmptyTile : Tile
    {
        // TODO
        // public event Action Ruined;

        public override bool IsComplete => true;

        public override bool IsTraversable(bool editMode) => false;
    }
}
