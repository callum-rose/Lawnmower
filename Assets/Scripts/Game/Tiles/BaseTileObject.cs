using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
    [Serializable]
    internal abstract class BaseTileObject<T> : MonoBehaviour, IDataObject<T> where T : Tile
    {
        public abstract void Setup(T data);

        public abstract void Dispose();
    }
}
