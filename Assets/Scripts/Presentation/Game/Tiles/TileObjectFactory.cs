using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Tiles
{
    internal class TileObjectFactory : MonoBehaviour
    {
        [FormerlySerializedAs("tilePrefabsHolder")] [SerializeField] private TilePrefabsManager tilePrefabsManager;

        private readonly InstancedTileRenderer _instancedRenderer = new InstancedTileRenderer();

        private void LateUpdate()
        {
            _instancedRenderer.Draw();
        }

        public GameObject Create<T>(T tile) where T : Tile
        {
            BaseTileObject newTileObject = tilePrefabsManager.GetPrefabAndInstantiate(tile);

            if (newTileObject == null)
            {
                return null;
            }
            
            newTileObject.Bind(tile);
            _instancedRenderer.Register(newTileObject.gameObject);
            
            return newTileObject.gameObject;
        }

        public void Remove(GameObject tileObject)
        {
            _instancedRenderer.Unregister(tileObject);
            Destroy(tileObject);
        }
    }
}
