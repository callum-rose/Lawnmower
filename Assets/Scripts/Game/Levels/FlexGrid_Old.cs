using System;
using System.Collections;
using System.Collections.Generic;

namespace Game.Levels
{
	public class FlexGrid_Old<T> : IEnumerable<T>
	{
		public int Height => _grid?[0].Span ?? 0;
		public int Width => _grid?.Span ?? 0;

		public int MaxCountY => _grid?[0].PositiveCount ?? 0;
		public int MinCountY => _grid?[0].NegativeCount ?? 0;
		public int MaxCountX => _grid?.PositiveCount ?? 0;
		public int MinCountX => _grid?.NegativeCount ?? 0;

		public T this[int x, int y]
		{
			get
			{
				if (x < MinCountX || x >= MaxCountX || y < MinCountY || y >= MaxCountY)
				{
					throw new IndexOutOfRangeException();
				}

				if (MaxCountX == 0 && MaxCountY == 0 && MinCountX == 0 && MinCountY == 0)
				{
					throw new IndexOutOfRangeException();
				}

				return _grid[x][y];
			}
			set
			{
				ExpandToInclude(x, y);
				_grid[x][y] = value;
			}
		}

		// BlockOfColumns<Column<Element>>
		private readonly BiDirectionList<BiDirectionList<T>> _grid;
		private Func<T> _defaultItemFactory;

		public FlexGrid_Old(Func<T> defaultItemFactory)
		{
			_grid = new BiDirectionList<BiDirectionList<T>>();
			_grid.AddToPositiveSide(new BiDirectionList<T>());

			_defaultItemFactory = defaultItemFactory;
		}

		public FlexGrid_Old(T[,] input, Func<T> defaultItemFactory) : this(defaultItemFactory)
		{
			ExpandRightToIncludeIndex(input.GetLength(0) - 1);
			ExpandUpToIncludeIndex(input.GetLength(1) - 1);

			for (int i = 0; i < MaxCountX; i++)
			{
				for (int j = 0; j < MaxCountY; j++)
				{
					this[i, j] = input[i, j];
				}
			}
		}

		public void ExpandToInclude(int x, int y)
		{
			ExpandUpToIncludeIndex(y);
			ExpandDownToIncludeIndex(y);

			ExpandRightToIncludeIndex(x);
			ExpandLeftToIncludeIndex(x);
		}

		private void ExpandUpToIncludeIndex(int maxY)
		{
			while (MaxCountY <= maxY)
			{
				AddRowToTop();
			}
		}

		private void ExpandDownToIncludeIndex(int minY)
		{
			while (MinCountY > minY)
			{
				AddRowToBottom();
			}
		}

		private void ExpandRightToIncludeIndex(int maxX)
		{
			while (MaxCountX <= maxX)
			{
				AddColumnToRight();
			}
		}

		private void ExpandLeftToIncludeIndex(int minX)
		{
			while (MinCountX > minX)
			{
				AddColumnToLeft();
			}
		}

		private void AddColumnToRight()
		{
			_grid.AddToPositiveSide(CreateColumn());
		}

		private void AddColumnToLeft()
		{
			_grid.AddToNegativeSide(CreateColumn());
		}

		private void AddRowToTop()
		{
			foreach (BiDirectionList<T> column in _grid)
			{
				T defaultItem = _defaultItemFactory.Invoke();
				column.AddToPositiveSide(defaultItem);
			}
		}

		private void AddRowToBottom()
		{
			foreach (BiDirectionList<T> column in _grid)
			{
				T defaultItem = _defaultItemFactory.Invoke();
				column.AddToNegativeSide(defaultItem);
			}
		}

		private BiDirectionList<T> CreateColumn()
		{
			BiDirectionList<T> newColumn = new BiDirectionList<T>();
			while (newColumn.PositiveCount < MaxCountY)
			{
				newColumn.AddToPositiveSide(default);
			}

			while (newColumn.NegativeCount > MinCountY)
			{
				newColumn.AddToNegativeSide(default);
			}

			return newColumn;
		}

		public IEnumerator<T> GetEnumerator()
		{
			foreach (BiDirectionList<T> column in _grid)
			{
				foreach (T item in column)
				{
					yield return item;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}