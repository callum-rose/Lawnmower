using System;
using Game.UndoSystem;

namespace Lawnmower.Solver
{
	/// <summary>
	/// Runs each undoable and forgets it, so a search can hold many simulations without each one
	/// carrying an undo history. Undo and Redo always return false.
	/// </summary>
	public sealed class NoHistoryUndoSystem : IUndoSystem
	{
		/// <summary>Stateless, so one instance can be shared across threads.</summary>
		public static readonly NoHistoryUndoSystem Instance = new NoHistoryUndoSystem();

		public int Count => 0;
		public int Limit => 0;

		public event Action Undone { add { } remove { } }
		public event Action Redone { add { } remove { } }

		public void Do(IUndoable undoable) => undoable.Do();
		public bool Redo() => false;
		public bool Undo() => false;
		public void Reset() { }
	}
}
