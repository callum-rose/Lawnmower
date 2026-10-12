using System;

namespace Game.Tiles
{
    [Serializable]
    internal class WoodTile : Tile, IReadonlyWoodTile
    {
        public override bool IsTraversable(bool editMode) => true;
        public override bool IsComplete => true;

        protected override string TypeKey => "wood";
    }
}