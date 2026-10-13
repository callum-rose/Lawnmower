using Game.Core;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class ObstacleRulesTests
	{
		[TestCase("stone:0")]
		[TestCase("stone:1")]
		[TestCase("stone:2")]
		[TestCase("stone:3")]
		[TestCase("water")]
		[TestCase("empty")]
		public void Bump_ChangesNothing_AndIsNotAnUndoStep(string obstacle)
		{
			LevelSimulation sim = Create($"grass:1 {obstacle} grass:2", 0, 0, out UndoSystem undo);
			SimulationSnapshot before = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Bumped, result.Outcome);
			Assert.IsFalse(result.ChangedState);
			Assert.AreEqual(V(0, 0), result.From);
			Assert.AreEqual(V(0, 0), result.To);
			Assert.AreEqual(V(1, 0), result.BumpedAt);
			CollectionAssert.AreEqual(new[] { V(0, 0) }, result.Path);
			CollectionAssert.IsEmpty(result.Segments);
			CollectionAssert.AreEqual(new[] { "bump 1,0 R", "bumped R at 1,0" }, events.Log);
			Assert.AreEqual(V(0, 0), sim.MowerPosition.Value);
			Assert.AreEqual(before, sim.Snapshot());
			Assert.IsFalse(undo.Undo(), "a bump is not an undo step");
		}

		[TestCase("stone:0")]
		[TestCase("water")]
		[TestCase("empty")]
		public void BumpAfterAMove_UndoRemovesTheMoveNotTheBump(string obstacle)
		{
			LevelSimulation sim = Create($"grass:1 grass:2 {obstacle} grass:2", 0, 0, out UndoSystem undo);
			SimulationSnapshot start = sim.Snapshot();

			Assert.AreEqual(MoveOutcome.Moved, sim.Move(R).Outcome);
			Assert.AreEqual(MoveOutcome.Bumped, sim.Move(R).Outcome);

			SimulationRecorder events = new SimulationRecorder(sim);
			Assert.IsTrue(undo.Undo());
			Assert.AreEqual(start, sim.Snapshot());
			Assert.IsFalse(undo.Undo());

			Assert.IsTrue(undo.Redo());
			Assert.AreEqual(0, events.Count("bump"), "neither undo nor redo replays a bump");
		}

		[TestCase(0, 1)]
		[TestCase(0, -1)]
		[TestCase(-1, 0)]
		[TestCase(1, 0)]
		public void OutOfBounds_OnEveryEdge_DoesNothing(int x, int y)
		{
			LevelSimulation sim = Create("grass:2", 0, 0, out UndoSystem undo);
			SimulationSnapshot before = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult result = sim.Move(new GridVector(x, y));

			Assert.AreEqual(MoveOutcome.OutOfBounds, result.Outcome);
			Assert.AreEqual(V(0, 0), result.To);
			Assert.IsNull(result.BumpedAt);
			CollectionAssert.IsEmpty(events.Log);
			Assert.AreEqual(before, sim.Snapshot());
			Assert.IsFalse(undo.Undo());
		}

		[Test]
		public void OutOfBounds_FromAnInnerEdgeTile()
		{
			LevelSimulation sim = Create("grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2", 2, 1);
			Assert.AreEqual(MoveOutcome.OutOfBounds, sim.Move(R).Outcome);
			Assert.AreEqual(MoveOutcome.OutOfBounds, sim.Move(U).Outcome);
			Assert.AreEqual(MoveOutcome.Moved, sim.Move(L).Outcome);
		}

		[Test]
		public void BumpWhileFailed_ReturnsBumped_AndStaysFailed()
		{
			LevelSimulation sim = Create("grass:2 grass:1 stone:0");
			sim.Move(R);
			Assert.AreEqual(LevelStatus.Failed, sim.Status);

			MoveResult bump = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Bumped, bump.Outcome);
			Assert.AreEqual(LevelStatus.Failed, bump.StatusBefore);
			Assert.AreEqual(LevelStatus.Failed, bump.StatusAfter);
			Assert.AreEqual(LevelStatus.Failed, sim.Status);
		}

		[Test]
		public void BumpedInto_DirectionIsTheMoveDirection()
		{
			LevelSimulation sim = Create("grass:2 grass:2 / stone:0 grass:2", 1, 1);
			GridVector? received = null;
			bool? inverted = null;
			sim.Level.GetTile(0, 1).BumpedInto += (dir, inv) =>
			{
				received = dir;
				inverted = inv;
			};

			sim.Move(L);

			Assert.AreEqual(L, received);
			Assert.AreEqual(false, inverted);
		}
	}
}
