using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
    [Serializable]
    internal abstract class BaseTileObject : MonoBehaviour, IDataObject<Tile>
    {
        public abstract void Bind(Tile data);

        public abstract void Dispose();
    }
}
