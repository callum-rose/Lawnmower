using System.Collections.Generic;
using Game.Core;
using Game.UndoSystem;
using NUnit.Framework;

namespace Lawnmower.Model.Tests
{
	/// <summary>
	/// The undo round-trip property: play the moves, undo everything (each step back must match the
	/// recorded snapshot), then redo everything (each step must match again). StatusChanged events
	/// must chain (each "from" is the previous "to") and always agree with the live status.
	/// </summary>
	public static class UndoRoundTrip
	{
		public static void Check(LevelSimulation sim, IUndoSystem undo, IReadOnlyList<GridVector> moves)
		{
			LevelStatus tracked = sim.Status;
			sim.StatusChanged += (from, to, inv) =>
			{
				Assert.AreEqual(tracked, from, "StatusChanged 'from' must be the previous status");
				tracked = to;
			};

			List<SimulationSnapshot> forward = new List<SimulationSnapshot> { sim.Snapshot() };

			for (int i = 0; i < moves.Count; i++)
			{
				MoveResult result = sim.Move(moves[i]);
				if (result.Outcome == MoveOutcome.Moved)
				{
					forward.Add(sim.Snapshot());
				}
				else
				{
					Assert.AreEqual(forward[forward.Count - 1], sim.Snapshot(), $"move {i} ({result.Outcome}) changed state");
				}

				Assert.AreEqual(tracked, sim.Status, $"status events out of step after move {i}");
			}

			for (int k = forward.Count - 2; k >= 0; k--)
			{
				Assert.IsTrue(undo.Undo(), $"undo back to step {k}");
				Assert.AreEqual(forward[k], sim.Snapshot(), $"undo back to step {k}");
				Assert.AreEqual(tracked, sim.Status, $"status events out of step at undo {k}");
			}

			Assert.IsFalse(undo.Undo(), "history should be exhausted");
			Assert.AreEqual(forward[0].Status, sim.Status);

			for (int k = 1; k < forward.Count; k++)
			{
				Assert.IsTrue(undo.Redo(), $"redo to step {k}");
				Assert.AreEqual(forward[k], sim.Snapshot(), $"redo to step {k}");
				Assert.AreEqual(tracked, sim.Status, $"status events out of step at redo {k}");
			}

			Assert.IsFalse(undo.Redo(), "redo should be exhausted");
		}
	}
}
