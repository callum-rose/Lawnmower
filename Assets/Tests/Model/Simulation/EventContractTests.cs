using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	/// <summary>
	/// The event sequence views consume. Step 5 may change these deliberately (e.g. moving views
	/// from per-segment position events to Moved/Path for B3); nothing else should.
	/// Per segment on Do: position, then away(from), then onto(to) (a grass change comes just
	/// before its onto). Then any end-of-path bump, then StatusChanged, then Moved.
	/// Undo walks the segments last-first: away, onto, then position; then StatusChanged, then Moved.
	/// </summary>
	[Category("ViewContract")]
	public class EventContractTests
	{
		[Test]
		public void Step_Do_Undo_Redo()
		{
			LevelSimulation sim = Create("grass:1 grass:2 grass:2", 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);
			string[] doLog =
			{
				"pos 0,0->1,0",
				"away 0,0 R",
				"grass 1,0 2->1",
				"onto 1,0 R",
				"moved R 0,0->1,0"
			};

			sim.Move(R);
			CollectionAssert.AreEqual(doLog, events.Log);

			events.Clear();
			undo.Undo();
			CollectionAssert.AreEqual(new[]
			{
				"away 0,0 R inv",
				"grass 1,0 1->2 inv",
				"onto 1,0 R inv",
				"pos 1,0->0,0 inv",
				"moved R 0,0->1,0 inv"
			}, events.Log);

			events.Clear();
			undo.Redo();
			CollectionAssert.AreEqual(doLog, events.Log, "redo raises the same sequence as the original move");
		}

		[Test]
		public void StatusChange_ComesAfterTheTiles_AndBeforeMoved()
		{
			LevelSimulation sim = Create("grass:1 grass:2", 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(R);
			CollectionAssert.AreEqual(new[]
			{
				"pos 0,0->1,0",
				"away 0,0 R",
				"grass 1,0 2->1",
				"onto 1,0 R",
				"status Playing->Completed",
				"moved R 0,0->1,0"
			}, events.Log);

			events.Clear();
			undo.Undo();
			CollectionAssert.AreEqual(new[]
			{
				"away 0,0 R inv",
				"grass 1,0 1->2 inv",
				"onto 1,0 R inv",
				"pos 1,0->0,0 inv",
				"status Completed->Playing inv",
				"moved R 0,0->1,0 inv"
			}, events.Log);
		}

		[Test]
		public void Spring_RaisesOnePositionChangePerSegment_EachWay()
		{
			LevelSimulation sim = Create("grass:1 spring:0,1 / grass:2 grass:2", 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);
			string[] doLog =
			{
				"pos 0,0->1,0",
				"away 0,0 R",
				"onto 1,0 R",
				"pos 1,0->1,1",
				"away 1,0 U",
				"grass 1,1 2->1",
				"onto 1,1 U",
				"moved R 0,0->1,1"
			};

			sim.Move(R);
			CollectionAssert.AreEqual(doLog, events.Log);

			events.Clear();
			undo.Undo();
			CollectionAssert.AreEqual(new[]
			{
				"away 1,0 U inv",
				"grass 1,1 1->2 inv",
				"onto 1,1 U inv",
				"pos 1,1->1,0 inv",
				"away 0,0 R inv",
				"onto 1,0 R inv",
				"pos 1,0->0,0 inv",
				"moved R 0,0->1,1 inv"
			}, events.Log);

			events.Clear();
			undo.Redo();
			CollectionAssert.AreEqual(doLog, events.Log);
		}

		[Test]
		public void LongJump_ReportsTheRawOffsetAsTheDirection()
		{
			LevelSimulation sim = Create("grass:1 spring:2,0 grass:2 grass:2");
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(R);

			CollectionAssert.Contains(events.Log, "away 1,0 (2,0)");
			CollectionAssert.Contains(events.Log, "onto 3,0 (2,0)");
		}

		[Test]
		public void SpringIntoABlockedLanding_BumpsAfterThePath()
		{
			LevelSimulation sim = Create("grass:1 spring:1,0 stone:0 grass:2", 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(R);
			CollectionAssert.AreEqual(new[]
			{
				"pos 0,0->1,0",
				"away 0,0 R",
				"onto 1,0 R",
				"bump 2,0 R",
				"moved R 0,0->1,0"
			}, events.Log);

			events.Clear();
			undo.Undo();
			CollectionAssert.AreEqual(new[]
			{
				"away 0,0 R inv",
				"onto 1,0 R inv",
				"pos 1,0->0,0 inv",
				"moved R 0,0->1,0 inv"
			}, events.Log);
		}

		[Test]
		public void Bump_RaisesTileBumpThenBumped_AndNothingElse()
		{
			LevelSimulation sim = Create("grass:1 water grass:2");
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(R);

			CollectionAssert.AreEqual(new[] { "bump 1,0 R", "bumped R at 1,0" }, events.Log);
		}

		[Test]
		public void OutOfBounds_RaisesNothing()
		{
			LevelSimulation sim = Create("grass:1 grass:2");
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(L);
			sim.Move(U);
			sim.Move(D);

			CollectionAssert.IsEmpty(events.Log);
		}

		[Test]
		public void Rejected_RaisesNothing()
		{
			LevelSimulation sim = Create("grass:1 grass:2 water");
			sim.Move(R);
			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(L);
			sim.Move(R);
			sim.Move(U);

			CollectionAssert.IsEmpty(events.Log);
		}
	}
}
