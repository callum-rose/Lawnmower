using System;

namespace Game.Tiles
{
    internal class EmptyTile : Tile
    {
        public event Action Ruined;

        public override bool IsComplete => true;

        public override bool IsTraversable(bool editMode) => false;
    }
}
