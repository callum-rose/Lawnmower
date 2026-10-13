using System.Collections.Generic;
using Game.Core;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class PreviewTests
	{
		private const string Mixed =
			"grass:1 grass:2 stone:0 grass:3 / wood spring:0,2 grass:2 water / grass:2 empty grass:3 spring:-3,0 / grass:3 grass:2 wood grass:1";

		private static IEnumerable<TestCaseData> EveryStartAndDirection()
		{
			LevelDefinition def = LevelText.Parse(Mixed, V(0, 0));
			foreach (GridVector p in Level.FromDefinition(def).Positions)
			{
				if (!TileFactory.Create(def[p]).IsTraversable)
				{
					continue;
				}

				foreach (GridVector d in Directions)
				{
					yield return new TestCaseData(p.x, p.y, d).SetName($"Preview_From_{p.x}_{p.y}_{MoveNotation.ToChar(d)}");
				}
			}
		}

		[TestCaseSource(nameof(EveryStartAndDirection))]
		public void Preview_ChangesNothing_AndMatchesTheMove(int x, int y, GridVector direction)
		{
			LevelSimulation sim = Create(Mixed, x, y, out UndoSystem undo);
			SimulationSnapshot before = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult preview = sim.Preview(direction);

			Assert.AreEqual(before, sim.Snapshot());
			CollectionAssert.IsEmpty(events.Log);
			Assert.IsFalse(undo.Undo());
			Assert.AreEqual(0, undo.Count);

			MoveResult move = sim.Move(direction);

			AssertSame(move, preview);
		}

		[Test]
		public void Preview_MidGame_AndWhileFailed_MatchesTheMove()
		{
			LevelSimulation sim = Create(Mixed, 0, 0, out UndoSystem undo);
			foreach (GridVector d in MoveNotation.Parse("RDURLLUUURRDDLR"))
			{
				SimulationSnapshot before = sim.Snapshot();
				int count = undo.Count;
				MoveResult preview = sim.Preview(d);
				Assert.AreEqual(before, sim.Snapshot());
				Assert.AreEqual(count, undo.Count);
				AssertSame(sim.Move(d), preview);
			}
		}

		[Test]
		public void DECISION_Preview_EvaluatesTheStatus()
		{
			LevelSimulation sim = Create("grass:1 grass:2");
			Assert.AreEqual(LevelStatus.Completed, sim.Preview(R).StatusAfter);
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
		}

		private static void AssertSame(MoveResult move, MoveResult preview)
		{
			Assert.AreEqual(move.Outcome, preview.Outcome, "Outcome");
			Assert.AreEqual(move.Direction, preview.Direction, "Direction");
			Assert.AreEqual(move.From, preview.From, "From");
			Assert.AreEqual(move.To, preview.To, "To");
			CollectionAssert.AreEqual(move.Path, preview.Path, "Path");
			CollectionAssert.AreEqual(move.Segments, preview.Segments, "Segments");
			Assert.AreEqual(move.BumpedAt, preview.BumpedAt, "BumpedAt");
			Assert.AreEqual(move.StatusBefore, preview.StatusBefore, "StatusBefore");
			Assert.AreEqual(move.StatusAfter, preview.StatusAfter, "StatusAfter");
		}
	}
}
