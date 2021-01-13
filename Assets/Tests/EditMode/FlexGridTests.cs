using System;
using Game.Levels;
using Moq;
using NUnit.Framework;
using UnityEngine;
using static UnityEngine.Mathf;

namespace Tests.EditMode
{
	public class FlexGridTests
	{
		[Test]
		public void ForEmptyGrid_WidthAndHeight_ReturnZero()
		{
			FlexGrid<object> grid = new FlexGrid<object>(It.IsAny<Func<It.IsAnyType>>());

			int width = grid.Bounds.width;
			int height = grid.Bounds.height;

			Assert.AreEqual(0, width);
			Assert.AreEqual(0, height);
		}

		[TestCase(new[] { 0, 0 })]
		[TestCase(new[] { 1, 1 })]
		[TestCase(new[] { -1, -1 })]
		[TestCase(new[] { 1, -1 })]
		[TestCase(new[] { -1, 1 })]
		public void SetElement_FirstIndex_ReturnsExpectedBounds(int[] point)
		{
			FlexGrid<object> grid = new FlexGrid<object>(() => new object());
			Vector2Int index = new Vector2Int(point[0], point[1]);
			RectInt expected = new RectInt(index.x, index.y, 1, 1);

			grid[index.x, index.y] = new object();

			RectInt result = grid.Bounds;
			Assert.IsTrue(result.Equals(expected));
		}

		[TestCase(new[] { 0, 0 }, new[] { 1, 1 })]
		[TestCase(new[] { 0, 0 }, new[] { -1, -1 })]
		[TestCase(new[] { 1, 1 }, new[] { -1, -1 })]
		[TestCase(new[] { 1, -1 }, new[] { -1, 1 })]
		[TestCase(new[] { -1, 1 }, new[] { 1, -1 })]
		public void SetElements_TwoIndicies_ReturnsExpectedBounds(int[] point0, int[] point1)
		{
			FlexGrid<object> grid = new FlexGrid<object>(() => new object());
			Vector2Int index0 = new Vector2Int(point0[0], point0[1]);
			Vector2Int index1 = new Vector2Int(point1[0], point1[1]);
			RectInt expected = new RectInt(Min(index0.x, index1.x), Min(index0.y, index1.y),
				Abs(index0.x - index1.x) + 1, Mathf.Abs(index0.y - index1.y) + 1);

			grid[index0.x, index0.y] = new object();
			grid[index1.x, index1.y] = new object();

			RectInt result = grid.Bounds;
			Assert.IsTrue(result.Equals(expected));
		}

		[TestCase(new[] { 0, 0 }, 99)]
		[TestCase(new[] { 1, 1 }, 99)]
		[TestCase(new[] { -1, -1 }, 99)]
		[TestCase(new[] { -1, 1 }, 99)]
		[TestCase(new[] { 1, -1 }, 99)]
		public void GetElement_FromIndexAfterSet_ReturnsExpectedResult(int[] point, int expected)
		{
			FlexGrid<int> grid = new FlexGrid<int>(() => 0);
			Vector2Int index = new Vector2Int(point[0], point[1]);
			grid[index.x, index.y] = expected;

			int result = grid[index.x, index.y];

			Assert.AreEqual(expected, result);
		}
		
		[Test]
		public void InputArrayConstructor_From2DArray_ReturnsSame()
		{
			object[,] input =
			{
				{ new object(), new object() }, 
				{ new object(), new object() }
			};
			FlexGrid<object> grid = new FlexGrid<object>(input, () => new object());

			for (int i = 0; i < input.GetLength(0); i++)
			{
				for (int j = 0; j < input.GetLength(1); j++)
				{
					object result = grid[i, j];
					object expected = input[i, j];
					Assert.AreEqual(expected, result);
				}
			}
		}

		[TestCase(new[] { 0, 0 })]
		[TestCase(new[] { 1, 1 })]
		[TestCase(new[] { -3, -5 })]
		public void GetElements_From2DArray_ReturnsSame(int[] offset)
		{
			object[,] input =
			{
				{ new object(), new object() }, 
				{ new object(), new object() }
			};
			FlexGrid<object> grid = new FlexGrid<object>(() => new object());

			for (int i = 0; i < input.GetLength(0); i++)
			{
				for (int j = 0; j < input.GetLength(1); j++)
				{
					grid[i + offset[0], j + offset[1]] = input[i, j];
				}
			}
			
			for (int i = 0; i < input.GetLength(0); i++)
			{
				for (int j = 0; j < input.GetLength(1); j++)
				{
					object result = grid[i + offset[0], j + offset[1]];
					object expected = input[i, j];
					Assert.AreEqual(expected, result);
				}
			}
		}
		
		[Test]
		public void GivenInputArrayConstructor_ExpandUp_ReturnsGridOneTaller()
		{
			object[,] input =
			{
				{ new object(), new object() }, 
				{ new object(), new object() }
			};
			FlexGrid<object> grid = new FlexGrid<object>(input, () => new object());

			RectInt expected = new RectInt(0, 0, 2, 3);
			
			grid.ExpandUp();

			Assert.IsTrue(expected.Equals(grid.Bounds));
		}	
		
		[Test]
		public void GivenInputArrayConstructor_ExpandDown_ReturnsGridOneDeeper()
		{
			object[,] input =
			{
				{ new object(), new object() }, 
				{ new object(), new object() }
			};
			FlexGrid<object> grid = new FlexGrid<object>(input, () => new object());

			RectInt expected = new RectInt(0, -1, 2, 3);
			
			grid.ExpandDown();

			Assert.IsTrue(expected.Equals(grid.Bounds));
		}	
		
		[Test]
		public void GivenInputArrayConstructor_ExpandRight_ReturnsGridOneWiderToRight()
		{
			object[,] input =
			{
				{ new object(), new object() }, 
				{ new object(), new object() }
			};
			FlexGrid<object> grid = new FlexGrid<object>(input, () => new object());

			RectInt expected = new RectInt(0, 0, 3, 2);
			
			grid.ExpandRight();

			Assert.IsTrue(expected.Equals(grid.Bounds));
		}	
		
		[Test]
		public void GivenInputArrayConstructor_ExpandRight_ReturnsGridOneWiderToLeft()
		{
			object[,] input =
			{
				{ new object(), new object() }, 
				{ new object(), new object() }
			};
			FlexGrid<object> grid = new FlexGrid<object>(input, () => new object());

			RectInt expected = new RectInt(-1, 0, 3, 2);
			
			grid.ExpandLeft();

			Assert.IsTrue(expected.Equals(grid.Bounds));
		}
	}
}