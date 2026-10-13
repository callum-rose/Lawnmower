using System;
using System.Linq;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class LevelTests
	{
		private const string Rows = "grass:1 grass:2 wood / stone:3 grass:3 spring:0,-1";
		private static readonly Guid Id = new Guid("3dbc7be08e344caa8b96bfda39edd55d");

		private static LevelDefinition Definition() => LevelText.Parse(Rows, V(0, 0), Id);

		[Test]
		public void FromDefinition_ToDefinition_RoundTrips()
		{
			LevelDefinition def = Definition();
			Assert.AreEqual(def, Level.FromDefinition(def).ToDefinition());
		}

		[Test]
		public void Id_SurvivesTheRoundTrip()
		{
			Level level = Level.FromDefinition(Definition());
			Assert.AreEqual(Id, level.Id);
			Assert.AreEqual(Id, level.ToDefinition().Id);
			Assert.AreEqual(Id, level.Clone().Id);
		}

		[Test]
		public void FromDefinition_CreatesFreshTilesEveryCall()
		{
			LevelDefinition def = Definition();
			Level a = Level.FromDefinition(def);
			Level b = Level.FromDefinition(def);

			for (int i = 0; i < a.Tiles.Count; i++)
			{
				Assert.AreNotSame(a.Tiles[i], b.Tiles[i]);
			}

			new LevelSimulation(a, new UndoSystem()).Move(R);

			Assert.AreEqual(1, ((Game.Tiles.GrassTile)a.GetTile(1, 0)).GrassHeight.Value);
			Assert.AreEqual(2, ((Game.Tiles.GrassTile)b.GetTile(1, 0)).GrassHeight.Value);
			Assert.AreEqual(Definition(), def, "the definition is never mutated");
		}

		[Test]
		public void Clone_IsIndependentOfTheOriginal()
		{
			Level original = Level.FromDefinition(Definition());
			Level clone = original.Clone();

			Assert.AreEqual(original.ToDefinition(), clone.ToDefinition());

			new LevelSimulation(clone, new UndoSystem()).Move(R);

			Assert.AreEqual(Definition(), original.ToDefinition());
			Assert.AreNotEqual(original.ToDefinition(), clone.ToDefinition());

			new LevelSimulation(original, new UndoSystem()).Move(R);
			Assert.AreEqual(original.ToDefinition(), clone.ToDefinition(), "same move, same state");
		}

		[Test]
		public void Clone_CopiesCurrentState()
		{
			Level level = Level.FromDefinition(Definition());
			new LevelSimulation(level, new UndoSystem()).Move(R);

			Assert.AreEqual(level.ToDefinition(), level.Clone().ToDefinition());
		}

		[Test]
		public void GetTile_IsNullOutsideTheBounds()
		{
			Level level = Level.FromDefinition(Definition());

			Assert.IsNull(level.GetTile(-1, 0));
			Assert.IsNull(level.GetTile(3, 0));
			Assert.IsNull(level.GetTile(0, 2));
			Assert.IsNull(level.GetTile(V(0, -1)));
			Assert.IsNotNull(level.GetTile(2, 1));
		}

		[Test]
		public void Tiles_AreRowMajor_AndPositionsCoverTheLevel()
		{
			Level level = Level.FromDefinition(Definition());

			Assert.AreEqual(6, level.Positions.Count());
			foreach (var p in level.Positions)
			{
				Assert.AreSame(level.Tiles[p.x + p.y * level.Width], level.GetTile(p));
			}
		}

		[Test]
		public void IsComplete_And_IsRuined()
		{
			Assert.IsFalse(Level.FromDefinition(Definition()).IsComplete);
			Assert.IsTrue(Level.FromDefinition(LevelText.Parse("grass:1 wood stone:0 / water empty spring:0,-1", V(0, 0))).IsComplete);
			Assert.IsTrue(Level.FromDefinition(LevelText.Parse("grass:0 grass:1", V(1, 0))).IsRuined);
			Assert.IsFalse(Level.FromDefinition(Definition()).IsRuined);
		}

		[Test]
		public void FromDefinition_Null_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => Level.FromDefinition(null));
		}
	}
}
