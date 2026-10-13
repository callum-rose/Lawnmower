using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Tiles;
using Game.UndoSystem;
using Lawnmower.Model;
using NUnit.Framework;

namespace Tests.EditMode
{
	/// <summary>Minimal coverage of the step 3 simulation. Step 4 moves and expands these.</summary>
	public class LevelSimulationSmokeTests
	{
		private static readonly GridVector R = GridVector.Right;
		private static readonly GridVector L = GridVector.Left;
		private static readonly GridVector U = GridVector.Up;

		/// <summary>Single row of tiles, x = 0 first. g1/g2/g3 = grass, s = stone, w = water, wd = wood, e = empty, j+N = spring offset (N, 0).</summary>
		private static Level Row(int start, params string[] tiles)
		{
			TileSpec[] specs = tiles.Select(Parse).ToArray();
			return Level.FromDefinition(new LevelDefinition(Guid.NewGuid(), specs.Length, 1, new GridVector(start, 0), specs));
		}

		private static TileSpec Parse(string t)
		{
			switch (t)
			{
				case "s": return TileSpec.Stone(0);
				case "w": return TileSpec.Water;
				case "wd": return TileSpec.Wood;
				case "e": return TileSpec.Empty;
			}

			if (t.StartsWith("g"))
			{
				return TileSpec.Grass(int.Parse(t.Substring(1)));
			}

			if (t.StartsWith("j"))
			{
				return TileSpec.Spring(new GridVector(int.Parse(t.Substring(1)), 0));
			}

			throw new ArgumentException(t);
		}

		private static int Height(LevelSimulation sim, int x) => ((GrassTile)sim.Level.GetTile(x, 0)).GrassHeight.Value;

		[Test]
		public void AlreadyCompleteLevel_StartsCompleted_AndRejectsMoves()
		{
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "s"), new UndoSystem());

			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			Assert.AreEqual(MoveOutcome.Rejected, sim.Move(R).Outcome);
		}

		[Test]
		public void MowingEveryTile_Completes()
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "g2", "g2"), undo);

			Assert.AreEqual(MoveOutcome.Moved, sim.Move(R).Outcome);
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
			MoveResult last = sim.Move(R);

			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			Assert.AreEqual(LevelStatus.Completed, last.StatusAfter);
			Assert.AreEqual(2, undo.Count);
			Assert.AreEqual(MoveOutcome.Rejected, sim.Move(L).Outcome, "Completed rejects moves");
		}

		[Test]
		public void BumpingStone_ChangesNothing_AndIsNotAnUndoStep()
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "s", "g2"), undo);
			int bumps = 0, moves = 0, bumpEvents = 0;
			sim.Level.GetTile(1, 0).BumpedInto += (d, inv) => bumps++;
			sim.Moved += (r, inv) => moves++;
			sim.Bumped += r => bumpEvents++;

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Bumped, result.Outcome);
			Assert.AreEqual(new GridVector(1, 0), result.BumpedAt);
			Assert.AreEqual(new GridVector(0, 0), sim.MowerPosition.Value);
			Assert.AreEqual(0, undo.Count);
			Assert.AreEqual((1, 0, 1), (bumps, moves, bumpEvents));
		}

		[Test]
		public void MovingOffTheLevel_DoesNothing()
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "g2"), undo);

			Assert.AreEqual(MoveOutcome.OutOfBounds, sim.Move(L).Outcome);
			Assert.AreEqual(MoveOutcome.OutOfBounds, sim.Move(U).Outcome);
			Assert.AreEqual(0, undo.Count);
		}

		[Test]
		public void Spring_IsOneUndoStep_AndUndoReturnsToBeforeTheSpring()
		{
			// P(0) -> S(1) -> jump +2 -> L(3)
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "j2", "g2", "g2"), undo);
			List<(GridVector, GridVector, bool)> positions = new List<(GridVector, GridVector, bool)>();
			sim.MowerPosition.ValueChangedFromTo += (f, t, inv) => positions.Add((f, t, inv));

			MoveResult result = sim.Move(R);

			Assert.AreEqual(new GridVector(3, 0), sim.MowerPosition.Value);
			Assert.AreEqual(1, undo.Count, "B2: the spring launch is part of the same undo step");
			Assert.AreEqual(2, result.Segments.Count);
			Assert.AreEqual(MovementKind.Jump, result.Segments[1].Kind);
			CollectionAssert.AreEqual(new[] { new GridVector(0, 0), new GridVector(1, 0), new GridVector(3, 0) }, result.Path);
			Assert.AreEqual(1, Height(sim, 3));
			Assert.AreEqual(2, Height(sim, 2), "jumped over");

			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(new GridVector(0, 0), sim.MowerPosition.Value, "B1: one undo returns to before the spring");
			Assert.AreEqual(2, Height(sim, 3));
			Assert.AreEqual(1, undo.Count, "B1: undo must not push a new step");

			Assert.IsTrue(undo.Redo());
			Assert.AreEqual(new GridVector(3, 0), sim.MowerPosition.Value);
			Assert.AreEqual(1, Height(sim, 3));

			Assert.AreEqual(6, positions.Count, "one position change per segment, each way");
		}

		[Test]
		public void SpringWin_RaisesStatusChangedOnce()
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "j1", "g2"), undo);
			List<(LevelStatus, LevelStatus, bool)> changes = new List<(LevelStatus, LevelStatus, bool)>();
			sim.StatusChanged += (f, t, inv) => changes.Add((f, t, inv));

			sim.Move(R);
			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			CollectionAssert.AreEqual(new[] { (LevelStatus.Playing, LevelStatus.Completed, false) }, changes);

			undo.Undo();
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
			CollectionAssert.AreEqual(new[]
			{
				(LevelStatus.Playing, LevelStatus.Completed, false),
				(LevelStatus.Completed, LevelStatus.Playing, true)
			}, changes);
		}

		[Test]
		public void SpringIntoStone_RestsOnTheSpringAndBumps()
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "j1", "s", "g2"), undo);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			Assert.AreEqual(new GridVector(1, 0), sim.MowerPosition.Value);
			Assert.AreEqual(new GridVector(2, 0), result.BumpedAt);
		}

		[Test]
		public void SpringLoop_ThrowsBeforeChangingAnything()
		{
			UndoSystem undo = new UndoSystem();
			// S(1) -> +1 -> S(2) -> -1 -> S(1) ...
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "j1", "j-1", "g2"), undo);

			Assert.Throws<InvalidOperationException>(() => sim.Move(R));
			Assert.AreEqual(new GridVector(0, 0), sim.MowerPosition.Value);
			Assert.AreEqual(0, undo.Count);
		}

		[Test]
		public void MovesAreAllowedAfterFailing_AndUndoPastTheFailRaisesOnce()
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g2", "g1"), undo);
			List<(LevelStatus, LevelStatus, bool)> changes = new List<(LevelStatus, LevelStatus, bool)>();
			sim.StatusChanged += (f, t, inv) => changes.Add((f, t, inv));

			sim.Move(R); // (1) 1 -> 0: ruined
			Assert.AreEqual(LevelStatus.Failed, sim.Status);

			MoveResult after = sim.Move(L);
			Assert.AreEqual(MoveOutcome.Moved, after.Outcome, "Failed still accepts moves");
			Assert.AreEqual(LevelStatus.Failed, sim.Status);

			undo.Undo();
			Assert.AreEqual(LevelStatus.Failed, sim.Status);
			undo.Undo();
			Assert.AreEqual(LevelStatus.Playing, sim.Status);

			CollectionAssert.AreEqual(new[]
			{
				(LevelStatus.Playing, LevelStatus.Failed, false),
				(LevelStatus.Failed, LevelStatus.Playing, true)
			}, changes);
		}

		[Test]
		public void WoodIsTraversable_AndNotMowed()
		{
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "wd", "g2"), new UndoSystem());

			sim.Move(R);
			sim.Move(R);

			Assert.AreEqual(LevelStatus.Completed, sim.Status);
		}

		[Test]
		public void NonCardinalDirection_Throws()
		{
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "g2"), new UndoSystem());
			Assert.Throws<ArgumentException>(() => sim.Move(new GridVector(1, 1)));
			Assert.Throws<ArgumentException>(() => sim.Move(GridVector.Zero));
		}

		[Test]
		public void Preview_DoesNotChangeState()
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "g2"), undo);
			SimulationSnapshot before = sim.Snapshot();

			MoveResult preview = sim.Preview(R);

			Assert.AreEqual(LevelStatus.Completed, preview.StatusAfter);
			Assert.AreEqual(before, sim.Snapshot());
			Assert.AreEqual(0, undo.Count);
		}

		[Test]
		public void Snapshot_RoundTripsThroughUndo_AndCloneIsIndependent()
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Row(0, "g1", "g3", "g2"), undo);
			SimulationSnapshot start = sim.Snapshot();

			sim.Move(R);
			LevelSimulation clone = sim.Clone(new UndoSystem());
			Assert.AreEqual(sim.Snapshot(), clone.Snapshot());

			clone.Move(R);
			Assert.AreNotEqual(sim.Snapshot(), clone.Snapshot());

			undo.Undo();
			Assert.AreEqual(start, sim.Snapshot());
			Assert.AreEqual(start.GetHashCode(), sim.Snapshot().GetHashCode());
		}

		[Test]
		public void CreatingALevel_GivesFreshTilesEveryTime()
		{
			LevelDefinition definition = Row(0, "g1", "g2").ToDefinition();
			LevelSimulation first = new LevelSimulation(Level.FromDefinition(definition), new UndoSystem());
			first.Move(R);

			LevelSimulation second = new LevelSimulation(Level.FromDefinition(definition), new UndoSystem());
			Assert.AreEqual(2, Height(second, 1));
			Assert.AreEqual(TileSpec.Grass(2), definition[1, 0]);
		}

		[Test]
		public void MoveNotation_RoundTrips()
		{
			IReadOnlyList<GridVector> moves = MoveNotation.Parse("u D l\nR");
			CollectionAssert.AreEqual(new[] { GridVector.Up, GridVector.Down, GridVector.Left, GridVector.Right }, moves);
			Assert.AreEqual("UDLR", MoveNotation.Format(moves));
			Assert.IsFalse(MoveNotation.TryParse("UX", out _));
		}

		[Test]
		public void MultiUndo_UndoesInReverseOrder()
		{
			List<string> log = new List<string>();
			MultiUndo multi = new MultiUndo(
				new Undoable(() => log.Add("do1"), () => log.Add("undo1")),
				new Undoable(() => log.Add("do2"), () => log.Add("undo2")));

			multi.Do();
			multi.Undo();

			CollectionAssert.AreEqual(new[] { "do1", "do2", "undo2", "undo1" }, log);
		}

		[Test]
		public void UndoSystem_ThrowsWhenReentered()
		{
			UndoSystem undo = new UndoSystem();
			Assert.Throws<InvalidOperationException>(() =>
				undo.Do(new Undoable(() => undo.Do(new Undoable(() => { }, () => { })), () => { })));

			// The guard is released after the failure.
			Assert.DoesNotThrow(() => undo.Do(new Undoable(() => { }, () => { })));
		}
	}
}
