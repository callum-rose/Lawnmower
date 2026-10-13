using System.Collections.Generic;
using Game.UndoSystem;
using NUnit.Framework;

namespace Lawnmower.Model.Tests
{
	public class MultiUndoTests
	{
		[Test]
		public void Do_RunsChildrenInForwardOrder_Undo_InReverseOrder()
		{
			List<string> log = new List<string>();
			MultiUndo multi = new MultiUndo(Logged(log, "1"), Logged(log, "2"), Logged(log, "3"));

			multi.Do();
			multi.Undo();
			multi.Do();

			CollectionAssert.AreEqual(new[]
			{
				"do 1", "do 2", "do 3",
				"undo 3", "undo 2", "undo 1",
				"do 1", "do 2", "do 3"
			}, log);
		}

		[Test]
		public void InsideUndoSystem_IsOneStep()
		{
			List<string> log = new List<string>();
			UndoSystem undo = new UndoSystem();
			undo.Do(new MultiUndo(Logged(log, "1"), Logged(log, "2")));

			Assert.IsTrue(undo.Undo());
			Assert.IsFalse(undo.Undo());
			CollectionAssert.AreEqual(new[] { "do 1", "do 2", "undo 2", "undo 1" }, log);
		}

		[Test]
		public void Empty_DoesNothing()
		{
			MultiUndo multi = new MultiUndo();
			Assert.DoesNotThrow(multi.Do);
			Assert.DoesNotThrow(multi.Undo);
		}

		internal static IUndoable Logged(List<string> log, string name) =>
			new Undoable(() => log.Add("do " + name), () => log.Add("undo " + name));
	}
}
