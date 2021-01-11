using Game.Core;
using System.Collections;
using System.Collections.Generic;

namespace Game.Tiles
{
    internal class ReadOnlyTiles : IEnumerable<Tilee>
    {
        public int Depth { get; }
        public int Width { get; }

        private Tilee[,] _tiles;

        public Tilee this[int x, int y] => _tiles[x, y];

        public ReadOnlyTiles(Tilee[,] tiles)
        {
            _tiles = tiles;

            Width = tiles.GetLength(0);
            Depth = tiles.GetLength(1);
        }

        public Tilee GetTile(GridVector position)
        {
            return GetTile(position.x, position.y);
        }

        public Tilee GetTile(int x, int y)
        {
            return _tiles[x, y];
        }

        public IEnumerator<Tilee> GetEnumerator()
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

        public static implicit operator ReadOnlyTiles(Tilee[,] tiles)
        {
            return new ReadOnlyTiles(tiles);
        }
    }
}
