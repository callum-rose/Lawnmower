using System.Collections.Generic;
using Game.UndoSystem;
using Moq;
using NUnit.Framework;

namespace Lawnmower.Model.Tests
{
	public class UndoSystemTests
	{
		[Test]
		public void UndoSystemLimitEqualsInputLimit()
		{
			const int limit = 2;

			UndoSystem undoSystem = new UndoSystem(limit);

			Assert.AreEqual(undoSystem.Limit, limit, $"Undo system should only hold {limit}, but has {undoSystem.Count}");
		}

		[Test]
		public void UndoSystemLimitIsRespected()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();

			const int limit = 2;

			UndoSystem undoSystem = new UndoSystem(limit);
			undoSystem.Do(undoable.Object);
			undoSystem.Do(undoable.Object);
			undoSystem.Do(undoable.Object);

			Assert.AreEqual(undoSystem.Count, limit, $"Undo system should only hold {limit}, but has {undoSystem.Count}");
		}

		[Test]
		public void UndoableDoMethodIsInvokedOnceWhenAddedToUndoSystem()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();
		
			UndoSystem undoSystem = new UndoSystem();
			undoSystem.Do(undoable.Object);

			undoable.Verify(e => e.Do(), Times.Once);
		}

		[Test]
		public void OneUndoCausesUndoActionToBeInvokedOnce()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();
			undoable.Setup(e => e.Undo());

			UndoSystem undoSystem = new UndoSystem();
			undoSystem.Do(undoable.Object);
			undoSystem.Undo();

			undoable.Verify(e => e.Undo(), Times.Once);
		}

		[Test]
		public void OneUndoThenRedoCausesUndoAndDoActionsToBeInvokedOnceEach()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();

			UndoSystem undoSystem = new UndoSystem();
			undoSystem.Do(undoable.Object);
			undoSystem.Undo();

			undoable.Verify(e => e.Undo(), Times.Once);

			undoSystem.Redo();

			undoable.Verify(e => e.Do(), Times.Exactly(2));
		}

		[Test]
		public void ResetResultsInCountEquallingZero()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();

			UndoSystem undoSystem = new UndoSystem();
			undoSystem.Do(undoable.Object);
			undoSystem.Reset();

			Assert.AreEqual(undoSystem.Count, 0);
		}

		[Test]
		public void ResetDoesNotResultInUndoOrDoActionsBeingInvoked()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();

			UndoSystem undoSystem = new UndoSystem();
			undoSystem.Do(undoable.Object);

			undoSystem.Reset();

			undoable.Verify(e => e.Do(), Times.Once);
			undoable.Verify(e => e.Undo(), Times.Never);
		}

		[Test]
		public void UndoResultsInUndoEventBeingInvokedOnce()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();

			UndoSystem undoSystem = new UndoSystem();
			undoSystem.Do(undoable.Object);

			int undoneEventCount = 0;
			undoSystem.Undone += () => undoneEventCount++;

			undoSystem.Undo();

			Assert.AreEqual(undoneEventCount, 1);
		}

		[Test]
		public void RedoResultsInRedoEventBeingInvokedOnce()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();

			UndoSystem undoSystem = new UndoSystem();
			undoSystem.Do(undoable.Object);
			undoSystem.Undo();

			int redoneEventCount = 0;
			undoSystem.Redone += () => redoneEventCount++;

			undoSystem.Redo();

			Assert.AreEqual(redoneEventCount, 1);
		}

		[Test]
		public void UndoEventIsNotInvokedOnUndoWhenUndoSystemIsEmpty()
		{
			UndoSystem undoSystem = new UndoSystem();

			int undoneEventCount = 0;
			undoSystem.Undone += () => undoneEventCount++;

			undoSystem.Undo();

			Assert.AreEqual(undoneEventCount, 0);
		}

		[Test]
		public void UndoEventIsInvokedOnlyAsManyTimesAsTheCountOfTheUndoSystem()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();

			const int numUndosToAdd = 2;

			UndoSystem undoSystem = new UndoSystem();
			for (int i = 0; i < numUndosToAdd; i++)
			{
				undoSystem.Do(undoable.Object);
			}

			int undoneEventCount = 0;
			undoSystem.Undone += () => undoneEventCount++;

			for (int i = 0; i < numUndosToAdd * 2; i++)
			{
				undoSystem.Undo();
			}

			Assert.AreEqual(undoneEventCount, numUndosToAdd);
		}

		[Test]
		public void RedoEventIsNotInvokedOnRedoWhenUndoSystemIsEmpty()
		{
			UndoSystem undoSystem = new UndoSystem();

			int redoneEventCount = 0;
			undoSystem.Redone += () => redoneEventCount++;

			undoSystem.Redo();

			Assert.AreEqual(redoneEventCount, 0);
		}
	
		[Test]
		public void RedoEventIsInvokedOnlyAsManyTimesAsTheCountOfTheUndoSystem()
		{
			Mock<IUndoable> undoable = new Mock<IUndoable>();

			const int numUndosToAdd = 2;

			UndoSystem undoSystem = new UndoSystem();
			for (int i = 0; i < numUndosToAdd; i++)
			{
				undoSystem.Do(undoable.Object);
			}
		
			for (int i = 0; i < numUndosToAdd; i++)
			{
				undoSystem.Undo();
			}
		
			int redoneEventCount = 0;
			undoSystem.Redone += () => redoneEventCount++;
		
			for (int i = 0; i < numUndosToAdd * 2; i++)
			{
				undoSystem.Redo();
			}

			Assert.AreEqual(redoneEventCount, numUndosToAdd);
		}

		[Test]
		public void UndoAndRedo_OnEmptyHistory_ReturnFalse()
		{
			UndoSystem undoSystem = new UndoSystem();

			Assert.IsFalse(undoSystem.Undo());
			Assert.IsFalse(undoSystem.Redo());
		}

		[Test]
		public void NewDoAfterUndo_DiscardsRedoEntries()
		{
			List<string> log = new List<string>();
			UndoSystem undoSystem = new UndoSystem();
			undoSystem.Do(Logged(log, "a"));
			undoSystem.Do(Logged(log, "b"));

			Assert.IsTrue(undoSystem.Undo());
			undoSystem.Do(Logged(log, "c"));

			Assert.IsFalse(undoSystem.Redo(), "b's redo entry was discarded");
			Assert.AreEqual(2, undoSystem.Count);
			Assert.IsTrue(undoSystem.Undo());
			Assert.IsTrue(undoSystem.Undo());
			Assert.IsFalse(undoSystem.Undo());
			CollectionAssert.AreEqual(new[] { "do a", "do b", "undo b", "do c", "undo c", "undo a" }, log);
		}

		[Test]
		public void Limit_DropsTheOldestSteps()
		{
			List<string> log = new List<string>();
			UndoSystem undoSystem = new UndoSystem(2);
			undoSystem.Do(Logged(log, "a"));
			undoSystem.Do(Logged(log, "b"));
			undoSystem.Do(Logged(log, "c"));

			Assert.IsTrue(undoSystem.Undo());
			Assert.IsTrue(undoSystem.Undo());
			Assert.IsFalse(undoSystem.Undo(), "only the newest 2 steps are kept");
			CollectionAssert.AreEqual(new[] { "do a", "do b", "do c", "undo c", "undo b" }, log);

			Assert.IsTrue(undoSystem.Redo());
			Assert.IsTrue(undoSystem.Redo());
			Assert.IsFalse(undoSystem.Redo());
		}

		private static IUndoable Logged(List<string> log, string name) =>
			new Undoable(() => log.Add("do " + name), () => log.Add("undo " + name));
	}
}
