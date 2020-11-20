using System.Collections.Generic;
using UnityEngine;
using BalsamicBits.Extensions;
using Pool;

namespace Game.Tiles
{
    internal class TileFactory : MonoBehaviour
    {
        [SerializeField] private TilePrefabsHolder tilePrefabsHolder;

        private Dictionary<TileType, IPool<Tile>> _poolDict;

        private void Awake()
        {
            _poolDict = new Dictionary<TileType, IPool<Tile>>();
            foreach (var t in EnumExtensions.GetValues<TileType>())
            {
                var prefab = tilePrefabsHolder.GetPrefab(t);
                IPool<Tile> newPool = new MonoBehaviourPool<Tile>(prefab);
                _poolDict.Add(t, newPool);
            }
        }

        public Tile Create(TileData data)
        {
            //Tile newTile = _poolDict[data.Type].Get();

            Tile newTile = Instantiate(tilePrefabsHolder.GetPrefab(data.Type));
            newTile.Setup(data.Data);
            return newTile;
        }

        public void Remove(Tile tile)
        {
            //TileType type = tilePrefabsHolder.GetTileTypeForTile(tile);
            //_poolDict[type].Enpool(tile);

            Destroy(tile.gameObject);
        }
    }
}
