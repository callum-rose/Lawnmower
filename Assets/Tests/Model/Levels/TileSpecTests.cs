using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Tiles;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class TileSpecTests
	{
		private static IEnumerable<TileSpec> Catalog() => TileCatalog.All;

		[TestCaseSource(nameof(Catalog))]
		public void CatalogEntry_RoundTripsThroughFactory(TileSpec spec)
		{
			Tile tile = TileFactory.Create(spec);
			Assert.AreEqual(spec, tile.ToSpec());
			Assert.AreEqual(spec.Kind, tile.Kind);
		}

		[TestCaseSource(nameof(Catalog))]
		public void Clone_IsANewInstanceWithAnEqualSpec(TileSpec spec)
		{
			Tile tile = TileFactory.Create(spec);
			Tile clone = tile.Clone();

			Assert.AreNotSame(tile, clone);
			Assert.AreEqual(tile.ToSpec(), clone.ToSpec());
		}

		[Test]
		public void TileCatalog_CoversEveryKind()
		{
			CollectionAssert.AreEquivalent(Enum.GetValues(typeof(TileKind)).Cast<TileKind>().ToArray(),
				TileCatalog.All.Select(s => s.Kind).Distinct().ToArray());
		}

		[Test]
		public void MowingAClonedLevel_DoesNotChangeTheOriginal()
		{
			Level original = Level.FromDefinition(LevelText.Parse("grass:1 grass:3", V(0, 0)));
			LevelSimulation onClone = new LevelSimulation(original.Clone(), new UndoSystem());

			onClone.Move(R);

			Assert.AreEqual(TileSpec.Grass(2), onClone.Level.GetTile(1, 0).ToSpec());
			Assert.AreEqual(TileSpec.Grass(3), original.GetTile(1, 0).ToSpec());
		}

		[TestCase("grass:1", true, true, false)]
		[TestCase("grass:2", true, false, false)]
		[TestCase("grass:3", true, false, false)]
		[TestCase("grass:0", true, false, true)]
		[TestCase("stone:0", false, true, false)]
		[TestCase("water", false, true, false)]
		[TestCase("empty", false, true, false)]
		[TestCase("wood", true, true, false)]
		[TestCase("spring:0,2", true, true, false)]
		public void DefaultFlags(string token, bool traversable, bool complete, bool ruined)
		{
			Tile tile = TileFactory.Create(LevelText.ParseTile(token));

			Assert.AreEqual(traversable, tile.IsTraversable, "IsTraversable");
			Assert.AreEqual(complete, tile.IsComplete, "IsComplete");
			Assert.AreEqual(ruined, tile.IsRuined, "IsRuined");
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public void StoneDirection_HasNoEffectOnFlags(int direction)
		{
			Tile stone = TileFactory.Create(TileSpec.Stone(direction));

			Assert.IsFalse(stone.IsTraversable);
			Assert.IsTrue(stone.IsComplete);
			Assert.IsFalse(stone.IsRuined);
			Assert.AreEqual(direction, ((IReadonlyStoneTile)stone).Direction);
		}

		[TestCase(4, 0)]
		[TestCase(-1, 3)]
		[TestCase(7, 3)]
		public void StoneDirection_WrapsIntoZeroToThree(int direction, int expected)
		{
			Assert.AreEqual(expected, TileSpec.Stone(direction).StoneDirection);
			Assert.AreEqual(expected, new StoneTile(direction).Direction);
		}

		[Test]
		public void SpringRedirect_IsPositionPlusOffset_AsAJump_WhateverTheArrivalDirection()
		{
			SpringTile spring = new SpringTile(V(0, -2));
			GridVector at = V(2, 2);

			foreach (GridVector arrival in Directions)
			{
				TileRedirect? redirect = spring.Redirect(at, arrival);
				Assert.IsTrue(redirect.HasValue);
				Assert.AreEqual(V(2, 0), redirect.Value.Target);
				Assert.AreEqual(MovementKind.Jump, redirect.Value.Kind);
			}
		}

		[TestCase("grass:2")]
		[TestCase("wood")]
		[TestCase("stone:1")]
		[TestCase("water")]
		[TestCase("empty")]
		public void OtherTiles_DoNotRedirect(string token)
		{
			Assert.IsNull(TileFactory.Create(LevelText.ParseTile(token)).Redirect(V(1, 1), R));
		}

		[Test]
		public void SpecEquality_ComparesKindAndState()
		{
			Assert.AreEqual(TileSpec.Grass(2), TileSpec.Grass(2));
			Assert.AreNotEqual(TileSpec.Grass(2), TileSpec.Grass(3));
			Assert.AreNotEqual(TileSpec.Stone(0), TileSpec.Stone(1));
			Assert.AreNotEqual(TileSpec.Spring(V(0, 2)), TileSpec.Spring(V(2, 0)));
			Assert.AreNotEqual(TileSpec.Water, TileSpec.Empty);
			Assert.IsTrue(TileSpec.Wood == TileSpec.Wood);
			Assert.AreEqual(TileSpec.Spring(V(1, -1)).GetHashCode(), TileSpec.Spring(V(1, -1)).GetHashCode());
		}

		[Test]
		public void SpecToString_UsesTheLevelTextGrammar()
		{
			foreach (TileSpec spec in TileCatalog.All)
			{
				Assert.AreEqual(spec, LevelText.ParseTile(spec.ToString()));
			}
		}

		[TestCase(4)]
		[TestCase(5)]
		[TestCase(-1)]
		public void B10_GrassHeightOutsideZeroToMax_ThrowsOnConstruction(int height)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new GrassTile(height));
			Assert.Throws<ArgumentOutOfRangeException>(() => TileFactory.Create(TileSpec.Grass(height)));
			Assert.Throws<ArgumentOutOfRangeException>(() =>
				Level.FromDefinition(LevelText.Parse($"grass:1 grass:{height}", V(0, 0))));
		}

		[Test]
		public void GrassHeight_ZeroIsAllowed_SoRuinedStateCanBeCloned()
		{
			GrassTile grass = new GrassTile(0);
			Assert.IsTrue(grass.IsRuined);
			Assert.AreEqual(TileSpec.Grass(0), grass.Clone().ToSpec());
		}
	}
}
