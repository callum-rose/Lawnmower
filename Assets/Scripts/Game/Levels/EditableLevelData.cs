using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Tiles;
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
			if (!_grid.PositionExists(x, y))
			{
				return null;
			}

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

		public void Trim()
		{
			RectInt newBounds = _grid.Bounds;

			foreach (int y in GetYIndices())
			{
				foreach (int x in GetXIndices())
				{
					Tile tile = GetTile(x, y);
					if (tile != null && !(tile is EmptyTile))
					{
						goto GoTo0;
					}
				}

				newBounds.yMin++;
			}

			GoTo0:

			foreach (int y in GetYIndices().Reverse())
			{
				foreach (int x in GetXIndices())
				{
					Tile tile = GetTile(x, y);
					if (tile != null && !(tile is EmptyTile))
					{
						goto GoTo1;
					}
				}

				newBounds.yMax--;
			}

			GoTo1:

			foreach (int x in GetXIndices())
			{
				foreach (int y in GetYIndices())
				{
					Tile tile = GetTile(x, y);
					if (tile != null && !(tile is EmptyTile))
					{
						goto GoTo2;
					}
				}

				newBounds.xMin++;
			}
			
			GoTo2:

			foreach (int x in GetXIndices().Reverse())
			{
				foreach (int y in GetYIndices())
				{
					Tile tile = GetTile(x, y);
					if (tile != null && !(tile is EmptyTile))
					{
						goto GoTo3;
					}
				}

				newBounds.xMax--;
			}
			
			GoTo3:

			Tile[,] newArray = new Tile[newBounds.width, newBounds.height];
			foreach (Vector2Int position in newBounds.allPositionsWithin)
			{
				Vector2Int posInNewArray = position - newBounds.min;
				newArray[posInNewArray.x, posInNewArray.y] = GetTile(position.x, position.y);
			}

			_grid = new FlexGrid<Tile>(newArray, () => new EmptyTile());
		}

		public IEnumerable<int> GetXIndices()
		{
			return _grid.GetXEnumerator();
		}

		public IEnumerable<int> GetYIndices()
		{
			return _grid.GetYEnumerator();
		}

		public IEnumerator<Tile> GetEnumerator()
		{
			return _grid.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public static EditableLevelData CreateFrom(IReadOnlyLevelData input)
		{
			EditableLevelData output = new EditableLevelData();

			Loops.TwoD(input.Width, input.Depth, (x, y) => output.SetTile(x, y, input.GetTile(x, y).Clone()));

			output.StartPosition = input.StartPosition;

			return output;
		}

		public void ValidateStartPos()
		{
			int x = Mathf.Min(Width - 1, Mathf.Max(0, StartPosition.x));
			int y = Mathf.Min(Depth - 1, Mathf.Max(0, StartPosition.y));
			StartPosition = new GridVector(x, y);
		}
	}
}