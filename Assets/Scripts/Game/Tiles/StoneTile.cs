using System;
using Newtonsoft.Json;

namespace Game.Tiles
{
    [Serializable]
    internal class StoneTile : Tile
    {
        public override bool IsTraversable(bool editMode) => false;
        public override bool IsComplete => true;

        public int Direction
        {
            get => _direction;
            set => _direction = value % 4;
        }

        private int _direction;
    }
}
