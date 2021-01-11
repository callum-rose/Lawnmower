using System.Collections;
using System.Collections.Generic;

namespace Game.Levels
{
	internal class FlexGrid<T>
	{
		// BlockOfColumns<Column<Element>>
		private readonly BiDirectionList<BiDirectionList<T>> _grid;

		public int Height => _grid?[0].Span ?? 0;
		public int Width => _grid?.Span ?? 0;

		public int MaxY => _grid?[0].MaxIndex ?? 0;
		public int MinY => _grid?[0].MinIndex ?? 0;
		public int MaxX => _grid?.MaxIndex ?? 0;
		public int MinX => _grid?.MinIndex ?? 0;

		public T this[int x, int y]
		{
			get => _grid[x][y];
			set => _grid[x][y] = value;
		}
		
		public FlexGrid()
		{
			_grid = new BiDirectionList<BiDirectionList<T>>();
			_grid.AddToPositiveSide(new BiDirectionList<T>());
		}

		public FlexGrid(T[,] input) : this()
		{
			ExpandRightTo(input.GetLength(0) - 1);
			ExpandUpTo(input.GetLength(1) - 1);

			for (int i = 0; i < MaxX; i++)
			{
				for (int j = 0; j < MaxY; j++)
				{
					this[i, j] = input[i, j];
				}
			}
		}

		public void ExpandToInclude(int x, int y)
		{
			ExpandUpTo(y);
			ExpandDownTo(y);
			
			ExpandRightTo(x);
			ExpandLeftTo(x);
		}

		private void ExpandUpTo(int maxY)
		{
			while (MaxY < maxY)
			{
				AddRowToTop();
			}
		}
		
		private void ExpandDownTo(int minY)
		{
			while (MinY > minY)
			{
				AddRowToBottom();
			}
		}
		
		private void ExpandRightTo(int maxX)
		{
			while (MaxX < maxX)
			{
				AddColumnToRight();
			}
		}
		
		private void ExpandLeftTo(int minX)
		{
			while (MinX > minX)
			{
				AddColumnToLeft();
			}
		}

		private void AddColumnToRight(T item = default)
		{
			_grid.AddToPositiveSide(CreateColumn());
		}

		private void AddColumnToLeft(T item = default)
		{
			_grid.AddToNegativeSide(CreateColumn());
		}

		private BiDirectionList<T> CreateColumn()
		{
			BiDirectionList<T> newColumn = new BiDirectionList<T>();
			for (int i = 0; i < _grid[0].MaxIndex; i++)
			{
				newColumn.AddToPositiveSide(default);
			}

			for (int i = 0; i > _grid[0].MinIndex; i--)
			{
				newColumn.AddToNegativeSide(default);
			}

			return newColumn;
		}

		private void AddRowToTop(T item = default)
		{
			foreach (BiDirectionList<T> column in _grid)
			{
				column.AddToPositiveSide(item);
			}
		}

		private void AddRowToBottom(T item = default)
		{
			foreach (BiDirectionList<T> column in _grid)
			{
				column.AddToNegativeSide(item);
			}
		}
	}

	internal class BiDirectionList<T> : IEnumerable<T>
	{
		public int MinIndex => -_negativeList.Count;
		public int MaxIndex => _positiveList.Count - 1;

		public int Span => MaxIndex - MinIndex;

		public T this[int i]
		{
			get => i >= 0 ? _positiveList[i] : _negativeList[GetIndexForNegative(i)];
			set
			{
				if (i >= 0)
				{
					_positiveList[i] = value;
				}
				else
				{
					_negativeList[GetIndexForNegative(i)] = value;
				}
			}
		}

		private List<T> _negativeList;
		private List<T> _positiveList;

		public BiDirectionList()
		{
			_negativeList = new List<T>();
			_positiveList = new List<T>();
		}

		public void AddToPositiveSide(T item)
		{
			_positiveList.Add(item);
		}

		public void AddToNegativeSide(T item)
		{
			_negativeList.Add(item);
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = MinIndex; i < MaxIndex; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private static int GetIndexForNegative(int i)
		{
			return -1 - i;
		}
	}
}