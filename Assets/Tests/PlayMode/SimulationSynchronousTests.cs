using System;
using System.Collections;
using Game.Core;
using Game.UndoSystem;
using Lawnmower.Model;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
	/// <summary>Replaces the MonoUndoSystem test: moves apply synchronously, so several in one frame chain (B8).</summary>
	public class SimulationSynchronousTests
	{
		[UnityTest]
		public IEnumerator TwoMovesInOneFrame_ChainFromTheLivePosition()
		{
			TileSpec[] row = { TileSpec.Grass(1), TileSpec.Grass(2), TileSpec.Grass(2) };
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(
				Level.FromDefinition(new LevelDefinition(Guid.NewGuid(), 3, 1, GridVector.Zero, row)), undo);

			sim.Move(GridVector.Right);
			sim.Move(GridVector.Right);
			Assert.AreEqual(new GridVector(2, 0), sim.MowerPosition.Value);
			Assert.AreEqual(LevelStatus.Completed, sim.Status);

			yield return null;

			Assert.AreEqual(2, undo.Count);
			undo.Undo();
			undo.Undo();
			Assert.AreEqual(GridVector.Zero, sim.MowerPosition.Value);
			Assert.AreEqual(LevelStatus.Playing, sim.Status);
		}
	}
}
