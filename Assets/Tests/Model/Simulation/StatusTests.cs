using Game.Core;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class StatusTests
	{
		[Test]
		public void IsPlaying_AtConstruction()
		{
			Assert.AreEqual(LevelStatus.Playing, Create("grass:1 grass:2").Status);
		}

		[Test]
		public void DECISION_AlreadyCompleteLevel_IsCompletedAtConstruction()
		{
			// Step 3 evaluates the status in the constructor (the step 4 plan expected Playing).
			Assert.AreEqual(LevelStatus.Completed, Create("grass:1").Status);
			Assert.AreEqual(LevelStatus.Completed, Create("grass:1 wood stone:0").Status);
		}

		[Test]
		public void DECISION_AlreadyRuinedLevel_IsFailedAtConstruction()
		{
			Assert.AreEqual(LevelStatus.Failed, Create("grass:2 grass:0").Status);
		}

		[Test]
		public void Completed_ExactlyWhenTheLastTileReachesOne()
		{
			LevelSimulation sim = Create("grass:1 grass:2 grass:2");
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult first = sim.Move(R);
			Assert.AreEqual(LevelStatus.Playing, first.StatusAfter);
			Assert.AreEqual(LevelStatus.Playing, sim.Status);

			MoveResult last = sim.Move(R);
			Assert.AreEqual(LevelStatus.Playing, last.StatusBefore);
			Assert.AreEqual(LevelStatus.Completed, last.StatusAfter);
			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			CollectionAssert.AreEqual(new[] { "status Playing->Completed" }, events.Log.FindAll(l => l.StartsWith("status")));
		}

		[Test]
		public void RuinedTile_IsNeverComplete_SoRuinAndCompletionCannotCoincide()
		{
			// The plan's "ruin beats completion" case can't happen: a move mows exactly one tile, and a
			// ruined tile (height 0) is not complete, so a ruining move never completes the level.
			LevelSimulation sim = Create("grass:2 grass:1 grass:1", 2, 0);

			sim.Move(L);

			Assert.IsTrue(sim.Level.GetTile(1, 0).IsRuined);
			Assert.IsFalse(sim.Level.GetTile(1, 0).IsComplete);
			Assert.AreEqual(LevelStatus.Failed, sim.Status);
			Assert.AreEqual(LevelStatus.Failed, Create("grass:0 grass:1").Status, "Failed, not Completed, at construction too");
		}

		[Test]
		public void MovesAreAllowedAfterFailed_WithoutStatusChanges()
		{
			LevelSimulation sim = Create("grass:2 grass:1 grass:3");
			sim.Move(R);
			Assert.AreEqual(LevelStatus.Failed, sim.Status);
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult after = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, after.Outcome);
			Assert.AreEqual(2, Height(sim, 2, 0), "tiles are still mowed");
			Assert.AreEqual(LevelStatus.Failed, sim.Status);
			Assert.AreEqual(LevelStatus.Failed, after.StatusBefore);
			Assert.AreEqual(LevelStatus.Failed, after.StatusAfter);
			Assert.AreEqual(0, events.Count("status"));
		}

		[Test]
		public void NoLaterMove_CanCompleteAFailedLevel()
		{
			// Ruin (1,0), then mow every other tile to 1: still Failed.
			LevelSimulation sim = Create("grass:2 grass:1 grass:2", 0, 0);
			sim.Move(R);
			sim.Move(R);
			sim.Move(L);
			sim.Move(L);

			Assert.AreEqual(1, Height(sim, 0, 0));
			Assert.AreEqual(1, Height(sim, 2, 0));
			Assert.AreEqual(LevelStatus.Failed, sim.Status);
		}

		[Test]
		public void UndoingAMoveMadeAfterFailed_KeepsFailed_WithoutStatusChanges()
		{
			LevelSimulation sim = Create("grass:2 grass:1 grass:3", 0, 0, out UndoSystem undo);
			Play(sim, "RR");
			SimulationRecorder events = new SimulationRecorder(sim);

			undo.Undo();

			Assert.AreEqual(LevelStatus.Failed, sim.Status);
			Assert.AreEqual(0, events.Count("status"));
		}

		[Test]
		public void UndoingTheFailingMove_RestoresPlaying_Once_AndRedoFailsAgain_Once()
		{
			LevelSimulation sim = Create("grass:2 grass:1 grass:3", 0, 0, out UndoSystem undo);
			sim.Move(R);
			SimulationRecorder events = new SimulationRecorder(sim);

			undo.Undo();
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
			undo.Redo();
			Assert.AreEqual(LevelStatus.Failed, sim.Status);

			CollectionAssert.AreEqual(new[] { "status Failed->Playing inv", "status Playing->Failed" },
				events.Log.FindAll(l => l.StartsWith("status")));
		}

		[TestCase(0, 1)]
		[TestCase(0, -1)]
		[TestCase(-1, 0)]
		[TestCase(1, 0)]
		public void Completed_RejectsEveryMove_IncludingBumpsAndOutOfBounds(int x, int y)
		{
			// Ends at (0,0): left and down are off the level, up is stone (a bump), right is grass.
			LevelSimulation sim = Create("grass:2 grass:2 grass:2 / stone:0 grass:2 grass:2", 1, 0, out UndoSystem undo);
			Play(sim, "RUL");
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
			sim.Move(D);
			Assert.AreEqual(MoveOutcome.Moved, sim.Move(L).Outcome);
			Assert.AreEqual(LevelStatus.Completed, sim.Status, "setup");
			SimulationSnapshot before = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			GridVector direction = new GridVector(x, y);
			MoveResult result = sim.Move(direction);
			MoveResult preview = sim.Preview(direction);

			Assert.AreEqual(MoveOutcome.Rejected, result.Outcome);
			Assert.AreEqual(MoveOutcome.Rejected, preview.Outcome);
			Assert.AreEqual(LevelStatus.Completed, result.StatusAfter);
			Assert.AreEqual(before, sim.Snapshot());
			CollectionAssert.IsEmpty(events.Log);
			Assert.AreEqual(5, UndoAll(undo), "no undo step was added");
		}

		[Test]
		public void UndoFromCompleted_AcceptsMovesAgain_AndANewMoveTruncatesRedo()
		{
			LevelSimulation done = Create("grass:2 grass:2 grass:1", 2, 0, out UndoSystem doneUndo);
			Play(done, "LL");
			Assert.AreEqual(LevelStatus.Completed, done.Status);

			Assert.IsTrue(doneUndo.Undo());
			Assert.AreEqual(LevelStatus.Playing, done.Status);
			Assert.AreEqual(MoveOutcome.Moved, done.Move(R).Outcome, "moves are accepted again");
			Assert.IsFalse(doneUndo.Redo(), "the new move truncated the redo entry");
		}
	}
}
