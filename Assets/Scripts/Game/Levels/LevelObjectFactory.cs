using Game.Core;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
    internal class LevelObjectFactory : MonoBehaviour
    {
        [SerializeField] private TileObjectFactory tileFactory;
        [SerializeField] private Positioner positioner;

        #region API

        public GameObject[,] Build(IReadOnlyLevelData level)
        {
            GameObject[,] tiles = new GameObject[level.Width, level.Depth];

            for (int y = 0; y < level.Depth; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    Tilee tile = level.GetTile(x, y);
                    GameObject newTileObject = BuildAt(new GridVector(x, y), tile);
                    tiles[x, y] = newTileObject;
                }
            }

            return tiles;
        }
        
        #endregion
        
        #region Methods

        private GameObject BuildAt(GridVector position, Tilee tile)
        {
            GameObject newTileObject = tileFactory.Create(tile);
            positioner.Position(newTileObject.transform, position);
#if UNITY_EDITOR
            gameObject.name = "Tile " + position;
#endif
            return newTileObject;
        }

        public void Destroy(GameObject[,] tileObjects)
        {
            if (tileObjects == null)
            {
                return;
            }

            foreach (var t in tileObjects)
            {
                Remove(t);
            }
        }

        public void Remove(GameObject tileObject)
        {
            tileFactory.Remove(tileObject);
        }

        #endregion
    }
}
