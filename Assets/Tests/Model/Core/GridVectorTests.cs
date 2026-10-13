using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Lawnmower.Model.Tests
{
	public class GridVectorTests
	{
		[Test]
		public void EqualVectors_HaveEqualHashes()
		{
			Assert.AreEqual(new GridVector(3, -2), new GridVector(3, -2));
			Assert.AreEqual(new GridVector(3, -2).GetHashCode(), new GridVector(3, -2).GetHashCode());
			Assert.AreNotEqual(new GridVector(3, -2), new GridVector(-2, 3));
		}

		[Test]
		public void Hashes_AreSpreadOut_AndWorkInAHashSet()
		{
			HashSet<int> hashes = new HashSet<int>();
			HashSet<GridVector> set = new HashSet<GridVector>();
			for (int x = 0; x < 7; x++)
			{
				for (int y = 0; y < 7; y++)
				{
					hashes.Add(new GridVector(x, y).GetHashCode());
					Assert.IsTrue(set.Add(new GridVector(x, y)));
				}
			}

			Assert.GreaterOrEqual(hashes.Count, 45);
			Assert.AreEqual(49, set.Count);
			Assert.IsTrue(set.Contains(new GridVector(6, 6)));
			Assert.IsFalse(set.Add(new GridVector(2, 5)));
		}

		[TestCase(0, 1, true)]
		[TestCase(0, -1, true)]
		[TestCase(1, 0, true)]
		[TestCase(-1, 0, true)]
		[TestCase(0, 0, false)]
		[TestCase(1, 1, false)]
		[TestCase(2, 0, false)]
		[TestCase(0, -2, false)]
		public void IsCardinalUnit(int x, int y, bool expected)
		{
			Assert.AreEqual(expected, new GridVector(x, y).IsCardinalUnit);
		}

		[Test]
		public void NamedDirections_MatchMoveNotation()
		{
			Assert.AreEqual(new GridVector(0, 1), GridVector.Up);
			Assert.AreEqual(new GridVector(0, -1), GridVector.Down);
			Assert.AreEqual(new GridVector(-1, 0), GridVector.Left);
			Assert.AreEqual(new GridVector(1, 0), GridVector.Right);
			Assert.AreEqual(new GridVector(0, 0), GridVector.Zero);
		}

		[Test]
		public void Magnitude_UsesSqrt()
		{
			Assert.AreEqual(5.0, new GridVector(3, 4).Magnitude, 1e-12);
			Assert.AreEqual(Math.Sqrt(2), new GridVector(-1, 1).Magnitude, 1e-12);
			Assert.AreEqual(0.0, GridVector.Zero.Magnitude);
		}

		[Test]
		public void Operators()
		{
			GridVector a = new GridVector(2, 3);
			GridVector b = new GridVector(-1, 4);

			Assert.AreEqual(new GridVector(1, 7), a + b);
			Assert.AreEqual(new GridVector(3, -1), a - b);
			Assert.AreEqual(new GridVector(-2, -3), -a);
			Assert.AreEqual(new GridVector(6, 9), a * 3);
			Assert.IsTrue(a == new GridVector(2, 3));
			Assert.IsTrue(a != b);
			Assert.IsFalse(a.Equals(null));
			Assert.IsTrue(a.Equals((object)new GridVector(2, 3)));
		}
	}
}
