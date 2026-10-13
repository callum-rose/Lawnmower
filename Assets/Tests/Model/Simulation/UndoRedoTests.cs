using System.Collections.Generic;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class UndoRedoTests
	{
		private const string Field = "grass:1 grass:3 grass:3 / grass:2 wood grass:3 / grass:3 grass:2 grass:2";

		[Test]
		public void UndoingEveryMove_WalksBackThroughEverySnapshot_InStackOrder()
		{
			LevelSimulation sim = Create(Field, 0, 0, out UndoSystem undo);
			List<SimulationSnapshot> states = new List<SimulationSnapshot> { sim.Snapshot() };

			foreach (var move in MoveNotation.Parse("RRUULLDR"))
			{
				MoveResult result = sim.Move(move);
				Assert.AreEqual(MoveOutcome.Moved, result.Outcome, result.ToString());
				states.Add(sim.Snapshot());
			}

			for (int k = states.Count - 2; k >= 0; k--)
			{
				Assert.IsTrue(undo.Undo());
				Assert.AreEqual(states[k], sim.Snapshot(), $"after undoing back to step {k}");
			}

			Assert.IsFalse(undo.Undo());

			for (int k = 1; k < states.Count; k++)
			{
				Assert.IsTrue(undo.Redo());
				Assert.AreEqual(states[k], sim.Snapshot(), $"after redoing step {k}");
			}

			Assert.IsFalse(undo.Redo());
		}

		[Test]
		public void NewMoveAfterUndo_DiscardsRedo()
		{
			LevelSimulation sim = Create(Field, 0, 0, out UndoSystem undo);
			Play(sim, "RR");
			undo.Undo();

			sim.Move(U);

			Assert.IsFalse(undo.Redo());
		}

		[Test]
		public void UndoLimit_KeepsOnlyTheNewestMoves()
		{
			UndoSystem undo = new UndoSystem(2);
			LevelSimulation sim = Create(LevelText.Parse(Field, V(0, 0)), undo);
			sim.Move(R);
			SimulationSnapshot afterFirst = sim.Snapshot();
			sim.Move(R);
			sim.Move(U);

			Assert.IsTrue(undo.Undo());
			Assert.IsTrue(undo.Undo());
			Assert.IsFalse(undo.Undo());
			Assert.AreEqual(afterFirst, sim.Snapshot());
		}

		[Test]
		public void Undo_OnAFreshSimulation_ReturnsFalse_AndRaisesNothing()
		{
			LevelSimulation sim = Create(Field, 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);

			Assert.IsFalse(undo.Undo());
			Assert.IsFalse(undo.Redo());
			CollectionAssert.IsEmpty(events.Log);
		}

		[Test]
		public void B8_TwoMovesInARow_ChainFromTheLivePosition()
		{
			LevelSimulation sim = Create(Field);

			sim.Move(R);
			sim.Move(R);

			Assert.AreEqual(V(2, 0), sim.MowerPosition.Value);
		}

		[Test]
		public void Moved_IsRaisedOnDoUndoAndRedo_WithTheSameResult()
		{
			LevelSimulation sim = Create(Field, 0, 0, out UndoSystem undo);
			List<(MoveResult result, bool inverted)> moved = new List<(MoveResult, bool)>();
			sim.Moved += (r, inv) => moved.Add((r, inv));

			MoveResult result = sim.Move(R);
			undo.Undo();
			undo.Redo();

			Assert.AreEqual(3, moved.Count);
			Assert.AreSame(result, moved[0].result);
			Assert.AreSame(result, moved[1].result);
			Assert.AreSame(result, moved[2].result);
			CollectionAssert.AreEqual(new[] { false, true, false }, moved.ConvertAll(m => m.inverted));
		}
	}
}
