using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class WoodRulesTests
	{
		[Test]
		public void Wood_CanBeWalkedOnto_AndIsComplete()
		{
			LevelSimulation sim = Create("grass:2 wood grass:2");

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			Assert.AreEqual(V(1, 0), sim.MowerPosition.Value);
			Assert.IsTrue(sim.Level.GetTile(1, 0).IsComplete);
			Assert.AreEqual(TileSpec.Wood, Spec(sim, 1, 0));
		}

		[Test]
		public void BackAndForth_ChangesNoHeightsAndNoStatus()
		{
			LevelSimulation sim = Create("wood wood grass:2", 0, 0, out UndoSystem undo);
			SimulationSnapshot start = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			for (int i = 0; i < 10; i++)
			{
				sim.Move(R);
				sim.Move(L);
			}

			Assert.AreEqual(start, sim.Snapshot());
			Assert.AreEqual(0, events.Count("grass"));
			Assert.AreEqual(0, events.Count("status"));
			Assert.AreEqual(LevelStatus.Playing, sim.Status);

			Assert.AreEqual(20, UndoAll(undo), "every crossing is its own undo step");
		}

		[Test]
		public void Wood_ReceivesTraversedOntoAndTraversedAway()
		{
			LevelSimulation sim = Create("grass:2 wood grass:2 grass:2");
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(R);
			sim.Move(R);

			CollectionAssert.Contains(events.Log, "onto 1,0 R");
			CollectionAssert.Contains(events.Log, "away 1,0 R");
		}
	}
}
