using System;
using System.Collections.Generic;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	/// <summary>One named case per bug from CODE_REVIEW.md that step 3 fixed.</summary>
	[Category("Regression")]
	public class BugRegressionTests
	{
		[Test]
		public void B1_SpringUndo_DoesNotRelaunch() => new SpringRulesTests().B1_Undo_DoesNotRelaunch();

		[Test]
		public void B2_SpringMove_IsSingleUndoStep() => new SpringRulesTests().B2_Jump_IsOneUndoStep();

		[Test]
		public void B4_SpringWin_StatusEvaluatedOnce()
		{
			LevelSimulation sim = Create("grass:1 spring:1,0 grass:2");
			int changes = 0;
			sim.StatusChanged += (from, to, inv) => changes++;

			sim.Move(R);

			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			Assert.AreEqual(1, changes);
		}

		[Test]
		public void B4_SpringFail_StatusEvaluatedOnce()
		{
			LevelSimulation sim = Create("grass:2 spring:1,0 grass:1");
			int changes = 0;
			sim.StatusChanged += (from, to, inv) => changes++;

			sim.Move(R);

			Assert.AreEqual(LevelStatus.Failed, sim.Status);
			Assert.AreEqual(1, changes);
		}

		[Test]
		public void B7_MultiUndo_ReverseOrder()
		{
			List<string> log = new List<string>();
			MultiUndo multi = new MultiUndo(MultiUndoTests.Logged(log, "1"), MultiUndoTests.Logged(log, "2"));

			multi.Do();
			multi.Undo();

			CollectionAssert.AreEqual(new[] { "do 1", "do 2", "undo 2", "undo 1" }, log);
		}

		[Test]
		public void B8_TwoMovesSameTick_ChainFromLivePosition() => new UndoRedoTests().B8_TwoMovesInARow_ChainFromTheLivePosition();

		[Test]
		public void B10_UndoCannotExceedMaxHeight()
		{
			// The only public route to a raw height above the max was constructing one; that now throws.
			Assert.Throws<ArgumentOutOfRangeException>(() => Create("grass:1 grass:4"));

			LevelSimulation sim = Create("grass:1 grass:3 wood", 0, 0, out UndoSystem undo);
			Play(sim, "RRL");
			Assert.AreEqual(3, UndoAll(undo));
			Assert.AreEqual(3, Height(sim, 1, 0));
		}
	}
}
