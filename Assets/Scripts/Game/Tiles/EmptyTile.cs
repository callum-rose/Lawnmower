using System;

namespace Game.Tiles
{
    [Serializable]
    internal class EmptyTile : Tile
    {
        // TODO
        // public event Action Ruined;

        public override bool IsComplete => true;

        protected override string TypeKey => "empty";

        public override bool IsTraversable(bool editMode) => false;
    }
}
