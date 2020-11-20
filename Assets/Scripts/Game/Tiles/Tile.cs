using Game.Core;
using Game.Tiles;
using Game.UndoSystem;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Tiles
{
    [Serializable]
    internal abstract class Tile : MonoBehaviour
    {
        public abstract bool IsTraversable(bool editMode);
        public abstract bool IsComplete { get; }

        public virtual void Setup(BaseTileSetupData data)
        {
            Assert.IsNull(data);
        }

        public virtual void TraverseOnto(GridVector fromDirection, Xor inversion)
        {

        }

        public virtual void TraverseAway(GridVector toDirection, Xor inversion)
        {

        }

        public virtual void BumpInto(GridVector fromDirection)
        {

        }
    }
}
