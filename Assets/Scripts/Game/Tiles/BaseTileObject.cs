using System;
using UnityEngine;

namespace Game.Tiles
{
    [Serializable]
    internal abstract class BaseTileObject : MonoBehaviour, IDataObject<IReadonlyTile>
    {
        public abstract void Bind(IReadonlyTile data);

        public abstract void Dispose();
    }
}
