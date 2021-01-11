using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Tiles;

namespace Game.Levels
{
	internal class EditableLevelData : ILevelData
	{
		public int Depth { get; set; }
		public int Width { get; set; }
		public GridVector StartPosition { get; }

		public Tilee GetTile(GridVector position)
		{
			throw new System.NotImplementedException();
		}

		public Tilee GetTile(int x, int y)
		{
			throw new System.NotImplementedException();
		}

		public void Init(Tilee[,] tiles, GridVector mowerStartPosition)
		{
			throw new System.NotImplementedException();
		}

		public void SetTile(int x, int y, Tilee tile)
		{
			throw new System.NotImplementedException();
		}

		public void SetTile(GridVector position, Tilee tile)
		{
			throw new System.NotImplementedException();
		}

		public IEnumerator<Tilee> GetEnumerator()
		{
			throw new System.NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}