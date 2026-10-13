using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class LevelDefinitionTests
	{
		private const string Rows = "grass:1 grass:2 wood / stone:1 water spring:0,-1";

		private static LevelDefinition Make(string rows = Rows, int sx = 0, int sy = 0, Guid? id = null) =>
			LevelText.Parse(rows, V(sx, sy), id);

		[Test]
		public void WrongTileCount_Throws()
		{
			TileSpec[] five = Enumerable.Repeat(TileSpec.Grass(2), 5).ToArray();
			Assert.Throws<ArgumentException>(() => new LevelDefinition(Guid.NewGuid(), 2, 3, V(0, 0), five));
			Assert.Throws<ArgumentException>(() => new LevelDefinition(Guid.NewGuid(), 2, 3, V(0, 0), null));
		}

		[TestCase(-1, 0)]
		[TestCase(0, -1)]
		[TestCase(3, 0)]
		[TestCase(0, 2)]
		public void StartOutsideTheLevel_Throws(int x, int y)
		{
			TileSpec[] tiles = Enumerable.Repeat(TileSpec.Grass(2), 6).ToArray();
			Assert.Throws<ArgumentException>(() => new LevelDefinition(Guid.NewGuid(), 3, 2, V(x, y), tiles));
		}

		[TestCase(0, 1)]
		[TestCase(1, 0)]
		[TestCase(-1, 2)]
		public void NonPositiveSize_Throws(int width, int depth)
		{
			Assert.Throws<ArgumentException>(() =>
				new LevelDefinition(Guid.NewGuid(), width, depth, V(0, 0), new TileSpec[0]));
		}

		[Test]
		public void Indexing_IsRowMajor()
		{
			LevelDefinition def = Make();
			for (int y = 0; y < def.Depth; y++)
			{
				for (int x = 0; x < def.Width; x++)
				{
					Assert.AreEqual(def.Tiles[x + y * def.Width], def[x, y]);
					Assert.AreEqual(def[x, y], def[V(x, y)]);
				}
			}

			Assert.AreEqual(TileSpec.Wood, def[2, 0]);
			Assert.AreEqual(TileSpec.Stone(1), def[0, 1]);
		}

		[Test]
		public void Contains()
		{
			LevelDefinition def = Make();
			Assert.IsTrue(def.Contains(V(0, 0)));
			Assert.IsTrue(def.Contains(V(2, 1)));
			Assert.IsFalse(def.Contains(V(3, 0)));
			Assert.IsFalse(def.Contains(V(0, 2)));
			Assert.IsFalse(def.Contains(V(-1, 0)));
		}

		[Test]
		public void EqualContent_IsEqual_WithTheSameHash()
		{
			LevelDefinition a = Make();
			LevelDefinition b = Make();

			Assert.AreNotSame(a, b);
			Assert.AreEqual(a, b);
			Assert.IsTrue(a.Equals((object)b));
			Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		}

		private static IEnumerable<TestCaseData> Differences()
		{
			yield return new TestCaseData(Make(id: Guid.NewGuid())).SetName("Different_Id");
			yield return new TestCaseData(Make(sx: 1)).SetName("Different_Start");
			yield return new TestCaseData(Make("grass:1 grass:2 / wood stone:1 / water spring:0,-1")).SetName("Different_Size");
			yield return new TestCaseData(Make("grass:1 grass:3 wood / stone:1 water spring:0,-1")).SetName("Different_GrassHeight");
			yield return new TestCaseData(Make("grass:1 grass:2 wood / stone:2 water spring:0,-1")).SetName("Different_StoneDirection");
			yield return new TestCaseData(Make("grass:1 grass:2 wood / stone:1 water spring:-1,0")).SetName("Different_SpringOffset");
			yield return new TestCaseData(Make("grass:1 grass:2 wood / stone:1 empty spring:0,-1")).SetName("Different_Kind");
		}

		[TestCaseSource(nameof(Differences))]
		public void AnyDifference_IsNotEqual(LevelDefinition other)
		{
			Assert.AreNotEqual(Make(), other);
		}

		[Test]
		public void IsImmutable_AgainstTheSourceList()
		{
			List<TileSpec> tiles = new List<TileSpec> { TileSpec.Grass(1), TileSpec.Grass(2) };
			LevelDefinition def = new LevelDefinition(Guid.NewGuid(), 2, 1, V(0, 0), tiles);

			tiles[1] = TileSpec.Water;

			Assert.AreEqual(TileSpec.Grass(2), def[1, 0]);
		}

		[Test]
		public void WithStartPosition_ChangesOnlyTheStart()
		{
			LevelDefinition def = Make();
			LevelDefinition moved = def.WithStartPosition(V(1, 0));

			Assert.AreEqual(V(1, 0), moved.StartPosition);
			Assert.AreEqual(def.Id, moved.Id);
			CollectionAssert.AreEqual(def.Tiles, moved.Tiles);
			Assert.AreEqual(V(0, 0), def.StartPosition);
			Assert.Throws<ArgumentException>(() => def.WithStartPosition(V(5, 5)));
		}
	}
}
