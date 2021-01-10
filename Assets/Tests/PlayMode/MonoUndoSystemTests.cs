using System.Collections;
using Game.UndoSystem;
using Moq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
	public class MonoUndoSystemTests
	{
		[UnityTest]
		public IEnumerator AllUndoablesAddedInTheSameFrameAreGroupedForUndo()
		{
			IUndoSystem undoSystem = new GameObject().AddComponent<MonoUndoSystem>();

			Mock<IUndoable> undoable = new Mock<IUndoable>();

			const int undoableCount = 3;

			for (int i = 0; i < undoableCount; i++)
			{
				undoSystem.Do(undoable.Object);
			}

			yield return null;

			undoSystem.Undo();
		
			undoable.Verify(e => e.Undo(), Times.Exactly(undoableCount));
		}
	
		[UnityTest]
		public IEnumerator AllUndoablesAddedInTheSameFrameAreGroupedForRedo()
		{
			IUndoSystem undoSystem = new GameObject().AddComponent<MonoUndoSystem>();

			Mock<IUndoable> undoable = new Mock<IUndoable>();

			const int undoableCount = 3;

			for (int i = 0; i < undoableCount; i++)
			{
				undoSystem.Do(undoable.Object);
			}

			yield return null;

			undoSystem.Undo();
			undoSystem.Redo();
		
			undoable.Verify(e => e.Do(), Times.Exactly(undoableCount * 2));
		}
	}
}