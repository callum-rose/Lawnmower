using System;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	/// <summary>
	/// Re-entering the undo system (or the simulation from its own events) is a programming error
	/// and must throw. Only the throw is asserted: state after it is unspecified.
	/// </summary>
	public class UndoReentrancyTests
	{
		private static readonly Action Nothing = () => { };

		[Test]
		public void Do_FromInsideDo_Throws()
		{
			UndoSystem undo = new UndoSystem();
			Assert.Throws<InvalidOperationException>(() =>
				undo.Do(new Undoable(() => undo.Do(new Undoable(Nothing, Nothing)), Nothing)));
		}

		[Test]
		public void Undo_FromInsideDo_Throws()
		{
			UndoSystem undo = new UndoSystem();
			undo.Do(new Undoable(Nothing, Nothing));
			Assert.Throws<InvalidOperationException>(() => undo.Do(new Undoable(() => undo.Undo(), Nothing)));
		}

		[Test]
		public void Redo_FromInsideUndo_Throws()
		{
			UndoSystem undo = new UndoSystem();
			undo.Do(new Undoable(Nothing, () => undo.Redo()));
			Assert.Throws<InvalidOperationException>(() => undo.Undo());
		}

		[Test]
		public void Do_FromInsideUndo_Throws()
		{
			UndoSystem undo = new UndoSystem();
			undo.Do(new Undoable(Nothing, () => undo.Do(new Undoable(Nothing, Nothing))));
			Assert.Throws<InvalidOperationException>(() => undo.Undo());
		}

		[Test]
		public void Guard_IsReleasedAfterTheThrow()
		{
			UndoSystem undo = new UndoSystem();
			Assert.Throws<InvalidOperationException>(() =>
				undo.Do(new Undoable(() => undo.Do(new Undoable(Nothing, Nothing)), Nothing)));

			Assert.DoesNotThrow(() => undo.Do(new Undoable(Nothing, Nothing)));
		}

		[Test]
		public void Move_FromInsideMovedHandler_Throws()
		{
			LevelSimulation sim = Create("grass:1 grass:2 grass:2 grass:2");
			sim.Moved += (result, inv) => sim.Move(R);

			Assert.Throws<InvalidOperationException>(() => sim.Move(R));
		}

		[Test]
		public void Move_FromInsideStatusChangedHandler_Throws()
		{
			LevelSimulation sim = Create("grass:2 grass:1 grass:2");
			sim.StatusChanged += (from, to, inv) => sim.Move(R);

			Assert.Throws<InvalidOperationException>(() => sim.Move(R));
		}

		[Test]
		public void Undo_FromInsideMovedHandler_Throws()
		{
			LevelSimulation sim = Create("grass:1 grass:2 grass:2", 0, 0, out UndoSystem undo);
			sim.Moved += (result, inv) => undo.Undo();

			Assert.Throws<InvalidOperationException>(() => sim.Move(R));
		}
	}
}
