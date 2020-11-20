using Game.Core;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
    internal class LevelFactory : MonoBehaviour
    {
        [SerializeField] private TileFactory tileFactory;
        [SerializeField] private Positioner positioner;

        #region API

        public Tile[,] Build(IReadOnlyLevelData level)
        {
            Tile[,] tiles = new Tile[level.Width, level.Depth];

            for (int y = 0; y < level.Depth; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    TileData data = level.GetTile(x, y);

                    Tile tile = BuildAt(new GridVector(x, y), data);

                    tiles[x, y] = tile;
                }
            }

            return tiles;
        }

        public Tile BuildAt(GridVector position, TileData data)
        {
            Tile tile = tileFactory.Create(data);
            positioner.Position(tile.transform, position);
#if UNITY_EDITOR
            tile.gameObject.name = "Tile " + position;
#endif
            return tile;
        }

        public void Destroy(Tile[,] tiles)
        {
            if (tiles == null)
            {
                return;
            }

            foreach (var t in tiles)
            {
                Destroy(t);
            }
        }

        public void Destroy(Tile tile)
        {
            tileFactory.Remove(tile);
        }

        #endregion
    }
}
