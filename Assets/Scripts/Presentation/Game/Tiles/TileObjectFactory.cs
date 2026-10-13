using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Tiles
{
    internal class TileObjectFactory : MonoBehaviour
    {
        [FormerlySerializedAs("tilePrefabsHolder")] [SerializeField] private TilePrefabsManager tilePrefabsManager;

        public GameObject Create<T>(T tile) where T : Tile
        {
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
            Destroy(tileObject);
        }
    }
}
