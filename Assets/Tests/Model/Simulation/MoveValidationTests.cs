using System;
using Game.Core;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class MoveValidationTests
	{
		[TestCase(0, 0)]
		[TestCase(1, 1)]
		[TestCase(2, 0)]
		[TestCase(0, -2)]
		public void NonCardinalMove_Throws_AndChangesNothing(int x, int y)
		{
			LevelSimulation sim = Create("grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2", 1, 1, out UndoSystem undo);
			SimulationSnapshot before = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			Assert.Throws<ArgumentException>(() => sim.Move(new GridVector(x, y)));

			Assert.AreEqual(before, sim.Snapshot());
			CollectionAssert.IsEmpty(events.Log);
			Assert.IsFalse(undo.Undo());
		}

		[TestCase(0, 0)]
		[TestCase(1, 1)]
		[TestCase(2, 0)]
		public void Preview_ValidatesTheSameWay(int x, int y)
		{
			LevelSimulation sim = Create("grass:2 grass:2", 0, 0);
			Assert.Throws<ArgumentException>(() => sim.Preview(new GridVector(x, y)));
		}

		[Test]
		public void NonCardinalMove_ThrowsEvenWhenCompleted()
		{
			LevelSimulation sim = Create("grass:1 wood");
			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			Assert.Throws<ArgumentException>(() => sim.Move(new GridVector(1, 1)));
		}

		[Test]
		public void Constructor_RejectsNulls()
		{
			Level level = Level.FromDefinition(LevelText.Parse("grass:1 grass:2", V(0, 0)));
			Assert.Throws<ArgumentNullException>(() => new LevelSimulation(null, new UndoSystem()));
			Assert.Throws<ArgumentNullException>(() => new LevelSimulation(level, null));
		}

		[Test]
		public void StartsAtTheStartPosition()
		{
			LevelSimulation sim = Create("grass:2 grass:2 / grass:2 grass:1", 1, 1);
			Assert.AreEqual(V(1, 1), sim.MowerPosition.Value);
		}
	}
}
