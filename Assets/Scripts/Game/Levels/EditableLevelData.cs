using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Game.Levels
{
	internal class EditableLevelData : IReadOnlyLevelData
	{
		public Guid Id => Guid.Empty;
		
		public int Depth => _grid.Bounds.height;
		public int Width => _grid.Bounds.width;

		public GridVector StartPosition { get; set; }

		private FlexGrid<Tile> _grid;

		public EditableLevelData()
		{
			_grid = new FlexGrid<Tile>(() => new EmptyTile());
		}

		public EditableLevelData(Tile[,] tiles, GridVector mowerStartPosition)
		{
			_grid = new FlexGrid<Tile>(tiles, () => new EmptyTile());
		}

		public Tile GetTile(GridVector position)
		{
			return GetTile(position.x, position.y);
		}

		public Tile GetTile(int x, int y)
		{
			return _grid[x, y];
		}

		public void SetTile(int x, int y, Tile tile)
		{
			_grid[x, y] = tile;
		}

		public void SetTile(GridVector position, Tile tile)
		{
			SetTile(position.x, position.y, tile);
		}

		public IEnumerator<Tile> GetEnumerator()
		{
			return _grid.GetEnumerator();
		}

		public void ExpandUp()
		{
			_grid.ExpandUp();
		}
		
		public void ExpandRight()
		{
			_grid.ExpandRight();
		}
		
		public void ExpandDown()
		{
			_grid.ExpandDown();
		}
		
		public void ExpandLeft()
		{
			_grid.ExpandLeft();
		}

		public IEnumerable<Vector2Int> GetIndices()
		{
			foreach (int y in _grid.GetYEnumerator())
			{
				foreach (int x in _grid.GetXEnumerator())
				{
					yield return new Vector2Int(x, y);
				}
			}
		}
		
		public IEnumerable<int> GetXIndices()
		{
			return _grid.GetXEnumerator();
		}
		
		public IEnumerable<int> GetYIndices()
		{
			return _grid.GetYEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public static EditableLevelData CreateFrom(LevelData input)
		{
			EditableLevelData output = new EditableLevelData();
			
			Loops.TwoD(input.Width, input.Depth, (x, y) => output.SetTile(x, y, input.GetTile(x, y)));

			output.StartPosition = input.StartPosition;
			
			return output;
		}
		
		internal void ValidateStartPos()
		{
			int x = Mathf.Min(Width - 1, Mathf.Max(0, StartPosition.x));
			int y = Mathf.Min(Depth - 1, Mathf.Max(0, StartPosition.y));
			StartPosition = new GridVector(x, y);
		}
	}
}