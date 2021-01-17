using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Levels
{
	public class FlexGrid<T> : IEnumerable<T>
	{
		public RectInt Bounds { get; private set; }
		private Vector2Int Offset => Bounds.min;

		private readonly Func<T> _defaultFactory;

		private T[,] _internalGrid;

		public T this[int x, int y]
		{
			get => GetElement(x, y, Offset, _internalGrid);
			set
			{
				ExpandToInclude(x, y);
				SetElement(x, y, Offset, _internalGrid, value);
			}
		}

		public FlexGrid(Func<T> defaultFactory)
		{
			_defaultFactory = defaultFactory;
			Bounds = new RectInt(0, 0, 0, 0);
		}

		public FlexGrid(T[,] input, Func<T> defaultFactory) : this(defaultFactory)
		{
			ExpandToInclude(0, 0);
			ExpandToInclude(GetArrayWidth(input) - 1, GetArrayHeight(input) - 1);

			for (int i = 0; i < GetArrayWidth(input); i++)
			{
				for (int j = 0; j < GetArrayHeight(input); j++)
				{
					this[i, j] = input[i, j];
				}
			}
		}

		public bool PositionExists(int x, int y)
		{
			return Bounds.Contains(new Vector2Int(x, y));
		}

		public void ExpandUp()
		{
			ExpandToInclude(Bounds.xMin, Bounds.yMax);
		}
		
		public void ExpandRight()
		{
			ExpandToInclude(Bounds.xMax, Bounds.yMin);
		}
		
		public void ExpandDown()
		{
			ExpandToInclude(Bounds.xMin, Bounds.yMin - 1);
		}
		
		public void ExpandLeft()
		{
			ExpandToInclude(Bounds.xMin - 1, Bounds.yMin);
		}

		public void ExpandToInclude(int x, int y)
		{
			if (Bounds.Contains(new Vector2Int(x, y)))
			{
				return;
			}

			RectInt oldBounds = Bounds;
			RectInt newBounds;
			if (oldBounds.width == 0 && oldBounds.height == 0)
			{
				newBounds = new RectInt(x, y, 1, 1);
			}
			else
			{
				newBounds = EncapsulatePositionInBounds(x, y, oldBounds);
			}

			T[,] oldGrid = _internalGrid;
			T[,] newGrid = new T[newBounds.width, newBounds.height];

			foreach (int i in GetXEnumerator(newBounds))
			{
				foreach (int j in GetYEnumerator(newBounds))
				{
					T element;
					if (oldBounds.Contains(new Vector2Int(i, j)))
					{
						element = GetElement(i, j, oldBounds.min, oldGrid);
					}
					else
					{
						element = _defaultFactory.Invoke();
					}

					SetElement(i, j, newBounds.min, newGrid, element);
				}
			}

			_internalGrid = newGrid;
			Bounds = newBounds;
		}

		private RectInt EncapsulatePositionInBounds(int x, int y, RectInt bounds)
		{
			RectInt newBounds = bounds;
			newBounds.xMin = Mathf.Min(x, newBounds.xMin);
			newBounds.xMax = Mathf.Max(x + 1, newBounds.xMax);
			newBounds.yMin = Mathf.Min(y, newBounds.yMin);
			newBounds.yMax = Mathf.Max(y + 1, newBounds.yMax);
			return newBounds;
		}

		private T GetElement(int x, int y, Vector2Int offset, T[,] array)
		{
			int offsetX = x - offset.x;
			int offsetY = y - offset.y;

			if (offsetX < 0 || offsetY < 0 || offsetX >= GetArrayWidth(array) || offsetY >= GetArrayHeight(array))
			{
				throw new IndexOutOfRangeException($"Index x: {x}, y: {y} is out of range.");
			}

			return array[offsetX, offsetY];
		}

		private void SetElement(int x, int y, Vector2Int offset, T[,] array, T value)
		{
			int offsetX = x - offset.x;
			int offsetY = y - offset.y;

			if (offsetX < 0 || offsetY < 0 || offsetX >= GetArrayWidth(array) || offsetY >= GetArrayHeight(array))
			{
				throw new IndexOutOfRangeException($"Index x: {x}, y: {y} is out of range.");
			}

			array[offsetX, offsetY] = value;
		}

		private int GetArrayWidth(T[,] array) => array.GetLength(0);

		private int GetArrayHeight(T[,] array) => array.GetLength(1);

		public IEnumerable<int> GetXEnumerator()
		{
			return GetXEnumerator(Bounds);
		}
		
		public IEnumerable<int> GetYEnumerator()
		{
			return GetYEnumerator(Bounds);
		}
		
		public IEnumerator<T> GetEnumerator()
		{
			return _internalGrid.Cast<T>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		private IEnumerable<int> GetXEnumerator(RectInt bounds)
		{
			for (int x = bounds.xMin; x < bounds.xMax; x++)
			{
				yield return x;
			}
		}
		
		private IEnumerable<int> GetYEnumerator(RectInt bounds)
		{
			for (int y = bounds.yMin; y < bounds.yMax; y++)
			{
				yield return y;
			}
		}
	}
}