using System.Collections;
using System.Collections.Generic;
using Game.Core;

namespace Game.Tiles
{
    internal class ReadOnlyTiles : IEnumerable<Tile>
    {
        public int Depth { get; }
        public int Width { get; }

        private Tile[,] _tiles;

        public Tile this[int x, int y] => _tiles[x, y];

        public ReadOnlyTiles(Tile[,] tiles)
        {
            _tiles = tiles;

            Width = tiles.GetLength(0);
            Depth = tiles.GetLength(1);
        }

        public Tile GetTile(GridVector position)
        {
            return GetTile(position.x, position.y);
        }

        public Tile GetTile(int x, int y)
        {
            return _tiles[x, y];
        }

        public IEnumerator<Tile> GetEnumerator()
        {
            for (int y = 0; y < Depth; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    yield return _tiles[x, y];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator ReadOnlyTiles(Tile[,] tiles)
        {
            return new ReadOnlyTiles(tiles);
        }
    }
}
