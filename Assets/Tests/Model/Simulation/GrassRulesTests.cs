using System;
using Game.Tiles;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class GrassRulesTests
	{
		[Test]
		public void HeightTwo_MowsToOne()
		{
			LevelSimulation sim = Create("grass:1 grass:2 grass:2");
			int moved = 0;
			sim.Moved += (r, inv) => moved++;

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			Assert.AreEqual(1, Height(sim, 1, 0));
			Assert.IsTrue(sim.Level.GetTile(1, 0).IsComplete);
			Assert.AreEqual(1, moved);
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
		}

		[Test]
		public void HeightThree_NeedsTwoVisits()
		{
			LevelSimulation sim = Create("grass:1 grass:3 wood");

			sim.Move(R);
			Assert.AreEqual(2, Height(sim, 1, 0));
			Assert.IsFalse(sim.Level.GetTile(1, 0).IsComplete);

			sim.Move(R);
			Assert.AreEqual(2, Height(sim, 1, 0));
			Assert.AreEqual(LevelStatus.Playing, sim.Status);

			sim.Move(L);
			Assert.AreEqual(1, Height(sim, 1, 0));
			Assert.IsTrue(sim.Level.GetTile(1, 0).IsComplete);
			Assert.AreEqual(LevelStatus.Completed, sim.Status);
		}

		[Test]
		public void MowingHeightOne_RuinsTheTile()
		{
			LevelSimulation sim = Create("grass:1 grass:1 grass:2");

			MoveResult result = sim.Move(R);

			Assert.AreEqual(0, Height(sim, 1, 0));
			Assert.IsTrue(sim.Level.GetTile(1, 0).IsRuined);
			Assert.IsFalse(sim.Level.GetTile(1, 0).IsComplete);
			Assert.AreEqual(LevelStatus.Failed, sim.Status);
			Assert.AreEqual(LevelStatus.Failed, result.StatusAfter);
		}

		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public void StartTile_IsNotMowedWhenTheLevelLoads(int height)
		{
			LevelSimulation sim = Create($"grass:{height} grass:2");
			Assert.AreEqual(height, Height(sim, 0, 0));
		}

		[Test]
		public void LeavingATile_DoesNotMowIt()
		{
			LevelSimulation sim = Create("grass:2 grass:2");

			sim.Move(R);

			Assert.AreEqual(2, Height(sim, 0, 0));
			Assert.AreEqual(1, Height(sim, 1, 0));
		}

		[Test]
		public void EachRevisit_MowsOnceMore_AndUndoRestoresStepByStep()
		{
			LevelSimulation sim = Create("grass:1 grass:3 grass:3", 0, 0, out UndoSystem undo);

			Play(sim, "RRL");
			Assert.AreEqual(new[] { 1, 1, 2 }, new[] { Height(sim, 0, 0), Height(sim, 1, 0), Height(sim, 2, 0) });

			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(new[] { 1, 2, 2 }, new[] { Height(sim, 0, 0), Height(sim, 1, 0), Height(sim, 2, 0) });
			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(new[] { 1, 2, 3 }, new[] { Height(sim, 0, 0), Height(sim, 1, 0), Height(sim, 2, 0) });
			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(new[] { 1, 3, 3 }, new[] { Height(sim, 0, 0), Height(sim, 1, 0), Height(sim, 2, 0) });
			Assert.IsFalse(undo.Undo());
		}

		[Test]
		public void MowingARuinedTile_StaysAtZero_AndUndoesBackThroughZero()
		{
			// After Failed, moves still mow; a ruined tile can't go below 0 visibly, but the history must unwind exactly.
			LevelSimulation sim = Create("grass:2 grass:1", 0, 0, out UndoSystem undo);

			Play(sim, "RLR");
			Assert.AreEqual(0, Height(sim, 1, 0));
			Assert.AreEqual(1, Height(sim, 0, 0));

			undo.Undo();
			Assert.AreEqual(0, Height(sim, 1, 0));
			undo.Undo();
			undo.Undo();
			Assert.AreEqual(1, Height(sim, 1, 0));
			Assert.AreEqual(2, Height(sim, 0, 0));
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
		}

		[Test]
		public void B10_GrassHeightAboveMax_ThrowsWhenTheLevelIsBuilt()
		{
			// Previously clamped on read only: a height-5 tile looked like 3, needed extra mows and its undo threw.
			Assert.Throws<ArgumentOutOfRangeException>(() => Create("grass:1 grass:5"));
			Assert.AreEqual(3, GrassTile.MaxGrassHeight);
		}
	}
}
