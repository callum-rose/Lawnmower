using System.Collections.Generic;
using Pool;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Tiles
{
    internal class TileObjectFactory : MonoBehaviour
    {
        [FormerlySerializedAs("tilePrefabsHolder")] [SerializeField] private TilePrefabsManager tilePrefabsManager;

        private Dictionary<TileType, IPool<Tile>> _poolDict;

        private void Awake()
        {
            // _poolDict = new Dictionary<TileType, IPool<Tilee>>();
            // foreach (TileType t in EnumExtensions.GetValues<TileType>())
            // {
            //     Tilee prefab = tilePrefabsHolder.GetPrefab(t);
            //     IPool<Tilee> newPool = new MonoBehaviourPool<Tilee>(prefab);
            //     _poolDict.Add(t, newPool);
            // }
        }

        public GameObject Create<T>(T tile) where T : Tile
        {
            //Tile newTile = _poolDict[data.Type].Get();

            BaseTileObject newTileObject = tilePrefabsManager.GetPrefabAndInstantiate(tile);

            if (newTileObject == null)
            {
                return null;
            }
            
            newTileObject.Bind(tile);
            
            return newTileObject.gameObject;
        }

        public void Remove(GameObject tileObject)
        {
            //TileType type = tilePrefabsHolder.GetTileTypeForTile(tile);
            //_poolDict[type].Enpool(tile);

            Destroy(tileObject);
        }
    }
}
