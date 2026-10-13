using System;
using System.Collections.Generic;
using Game.Core;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	/// <summary>P -> S -> L: the mower at P steps onto spring S and jumps to L = S + offset.</summary>
	public class SpringRulesTests
	{
		private const string Basic = "grass:1 spring:0,1 / grass:2 grass:2";
		private static readonly GridVector P = V(0, 0), S = V(1, 0), Landing = V(1, 1);

		[Test]
		public void BasicJump()
		{
			LevelSimulation sim = Create(Basic);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			CollectionAssert.AreEqual(new[] { P, S, Landing }, result.Path);
			Assert.AreEqual(2, result.Segments.Count);
			Assert.AreEqual((P, S, MovementKind.Step), (result.Segments[0].From, result.Segments[0].To, result.Segments[0].Kind));
			Assert.AreEqual((S, Landing, MovementKind.Jump), (result.Segments[1].From, result.Segments[1].To, result.Segments[1].Kind));
			Assert.AreEqual(Landing, result.To);
			Assert.AreEqual(Landing, sim.MowerPosition.Value);
			Assert.IsNull(result.BumpedAt);
			Assert.AreEqual(1, Height(sim, 1, 1), "the landing is mowed");
			Assert.AreEqual(2, Height(sim, 0, 1), "nothing else is");
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
		}

		[Test]
		public void B2_Jump_IsOneUndoStep()
		{
			LevelSimulation sim = Create(Basic, 0, 0, out UndoSystem undo);
			SimulationSnapshot initial = sim.Snapshot();

			sim.Move(R);
			Assert.IsTrue(undo.Undo());

			Assert.AreEqual(P, sim.MowerPosition.Value, "back at P, not S");
			Assert.AreEqual(2, Height(sim, 1, 1));
			Assert.AreEqual(initial, sim.Snapshot());
			Assert.IsFalse(undo.Undo());
		}

		[Test]
		public void B1_Undo_DoesNotRelaunch()
		{
			LevelSimulation sim = Create(Basic, 0, 0, out UndoSystem undo);
			sim.Move(R);
			SimulationRecorder events = new SimulationRecorder(sim);

			undo.Undo();

			CollectionAssert.AreEqual(new[] { "pos 1,1->1,0 inv", "pos 1,0->0,0 inv" }, events.Log.FindAll(l => l.StartsWith("pos")));
			Assert.AreEqual(P, sim.MowerPosition.Value);
			Assert.IsTrue(undo.Redo(), "undo pushed no new step, so the redo entry is intact");
			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(P, sim.MowerPosition.Value);
			Assert.IsFalse(undo.Undo());
		}

		[Test]
		public void Redo_ReplaysThePath()
		{
			LevelSimulation sim = Create(Basic, 0, 0, out UndoSystem undo);
			MoveResult original = sim.Move(R);
			SimulationSnapshot after = sim.Snapshot();
			undo.Undo();

			MoveResult replayed = null;
			sim.Moved += (r, inv) => replayed = r;
			SimulationRecorder events = new SimulationRecorder(sim);
			Assert.IsTrue(undo.Redo());

			Assert.AreEqual(original.Path, replayed.Path);
			Assert.AreEqual(original.Segments, replayed.Segments);
			Assert.AreEqual(original.Outcome, replayed.Outcome);
			Assert.AreEqual(after, sim.Snapshot());
			Assert.AreEqual(2, events.Count("pos"));
		}

		private static IEnumerable<TestCaseData> Approaches()
		{
			yield return new TestCaseData(0, 1, R).SetName("ArrivalFromLeft");
			yield return new TestCaseData(2, 1, L).SetName("ArrivalFromRight");
			yield return new TestCaseData(1, 0, U).SetName("ArrivalFromBelow");
			yield return new TestCaseData(1, 2, D).SetName("ArrivalFromAbove");
		}

		[TestCaseSource(nameof(Approaches))]
		public void ArrivalDirection_DoesNotChangeTheLanding(int x, int y, GridVector direction)
		{
			LevelSimulation sim = Create("grass:3 grass:3 grass:3 / grass:3 spring:1,1 grass:3 / grass:3 grass:3 grass:3", x, y);

			MoveResult result = sim.Move(direction);

			Assert.AreEqual(V(2, 2), result.To);
			Assert.AreEqual(2, Height(sim, 2, 2));
		}

		[Test]
		public void LandingOutOfBounds_RestsOnTheSpring_AsOneUndoStep()
		{
			LevelSimulation sim = Create("grass:1 spring:0,5 grass:2", 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			Assert.AreEqual(S, sim.MowerPosition.Value);
			CollectionAssert.AreEqual(new[] { P, S }, result.Path);
			Assert.IsNull(result.BumpedAt);
			Assert.AreEqual(0, events.Count("bump"));
			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(P, sim.MowerPosition.Value);
			Assert.IsFalse(undo.Undo());
		}

		[TestCase("stone:0")]
		[TestCase("water")]
		[TestCase("empty")]
		public void LandingBlocked_RestsOnTheSpring_AndBumpsTheLanding(string obstacle)
		{
			LevelSimulation sim = Create($"grass:1 spring:1,0 {obstacle} grass:2", 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			Assert.AreEqual(S, sim.MowerPosition.Value);
			Assert.AreEqual(V(2, 0), result.BumpedAt);
			CollectionAssert.AreEqual(new[] { "bump 2,0 R" }, events.Log.FindAll(l => l.StartsWith("bump")));
			Assert.AreEqual(0, events.Count("bumped"), "the Bumped event is only for moves that don't happen");

			events.Clear();
			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(0, events.Count("bump"), "undo raises no BumpedInto");
			Assert.AreEqual(P, sim.MowerPosition.Value);
		}

		[Test]
		public void LandingRuinsATile_FailsWithOneStatusChange()
		{
			LevelSimulation sim = Create("grass:2 spring:1,0 grass:1");
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(LevelStatus.Failed, sim.Status);
			Assert.AreEqual(LevelStatus.Failed, result.StatusAfter);
			CollectionAssert.AreEqual(new[] { "status Playing->Failed" }, events.Log.FindAll(l => l.StartsWith("status")));
		}

		[Test]
		public void LandingCompletesTheLevel_WithOneStatusChangeEachWay()
		{
			LevelSimulation sim = Create("grass:1 spring:1,0 grass:2", 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(R);
			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			undo.Undo();
			Assert.AreEqual(LevelStatus.Playing, sim.Status);

			CollectionAssert.AreEqual(new[] { "status Playing->Completed", "status Completed->Playing inv" },
				events.Log.FindAll(l => l.StartsWith("status")));
		}

		[Test]
		public void ChainedSprings_AreOneMoveAndOneUndoStep()
		{
			LevelSimulation sim = Create("grass:1 spring:1,0 spring:1,0 grass:2 grass:2", 0, 0, out UndoSystem undo);

			MoveResult result = sim.Move(R);

			CollectionAssert.AreEqual(new[] { V(0, 0), V(1, 0), V(2, 0), V(3, 0) }, result.Path);
			CollectionAssert.AreEqual(new[] { MovementKind.Step, MovementKind.Jump, MovementKind.Jump },
				new[] { result.Segments[0].Kind, result.Segments[1].Kind, result.Segments[2].Kind });
			Assert.AreEqual(1, Height(sim, 3, 0));
			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(V(0, 0), sim.MowerPosition.Value);
			Assert.IsFalse(undo.Undo());
		}

		[TestCase("grass:1 spring:1,0 spring:-1,0 grass:2")]
		[TestCase("grass:1 spring:0,0 grass:2")]
		[TestCase("grass:1 spring:1,0 spring:1,0 spring:-2,0 grass:2")]
		public void SpringCycle_ThrowsWhilePlanning_AndChangesNothing(string rows)
		{
			LevelSimulation sim = Create(rows, 0, 0, out UndoSystem undo);
			SimulationSnapshot before = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			Assert.Throws<InvalidOperationException>(() => sim.Move(R));
			Assert.Throws<InvalidOperationException>(() => sim.Preview(R));

			Assert.AreEqual(before, sim.Snapshot());
			CollectionAssert.IsEmpty(events.Log);
			Assert.IsFalse(undo.Undo());
		}

		[Test]
		public void ReenteringASpring_TriggersItAgain()
		{
			LevelSimulation sim = Create("grass:3 spring:1,0 stone:0");
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult first = sim.Move(R);
			sim.Move(L);
			MoveResult second = sim.Move(R);

			Assert.AreEqual(V(2, 0), first.BumpedAt);
			Assert.AreEqual(V(2, 0), second.BumpedAt);
			Assert.AreEqual(2, events.Count("bump 2,0"));
		}

		[Test]
		public void ReenteringASpring_AfterLanding_JumpsAgain()
		{
			// Spring at (1,0) lands on (1,2); walk back down onto the spring and it fires again.
			LevelSimulation sim = Create("grass:3 spring:0,2 / grass:3 grass:3 / grass:3 grass:3");

			Assert.AreEqual(V(1, 2), sim.Move(R).To);
			Assert.AreEqual(V(1, 1), sim.Move(D).To);
			Assert.AreEqual(V(1, 2), sim.Move(D).To, "stepping onto the spring from above fires it again");
			Assert.AreEqual(1, Height(sim, 1, 2));
		}

		[Test]
		public void SpringTile_NeverChangesState()
		{
			LevelSimulation sim = Create(Basic, 0, 0, out UndoSystem undo);
			TileSpec before = Spec(sim, 1, 0);

			sim.Move(R);
			Assert.AreEqual(before, Spec(sim, 1, 0));
			undo.Undo();
			Assert.AreEqual(before, Spec(sim, 1, 0));
			undo.Redo();
			Assert.AreEqual(before, Spec(sim, 1, 0));
		}
	}
}
