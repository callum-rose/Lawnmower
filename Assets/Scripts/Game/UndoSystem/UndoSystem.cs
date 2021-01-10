using System;
using System.Collections.Generic;

namespace Game.UndoSystem
{
	public class UndoSystem : IUndoSystem
	{
		public int Count => _undoables.Count;
		public int Limit { get; private set; }

		public event Action Undone;
		public event Action Redone;

		private bool HasLimit => Limit != 0 && Limit != int.MaxValue;
		
		private int _currentIndex;
		private List<IUndoable> _undoables;

		public UndoSystem()
		{
			Limit = int.MaxValue;
			Reset();
		}
		
		public UndoSystem(int limit)
		{
			Limit = limit;
			Reset();
		}

		#region API

		public void Do(IUndoable undoable)
		{
			while (_undoables.Count - 1 > _currentIndex)
			{
				_undoables.RemoveAt(_undoables.Count - 1);
			}

			_undoables.Add(undoable);
			_currentIndex++;

			if (HasLimit)
			{
				while (_undoables.Count > Limit)
				{
					_undoables.RemoveAt(0);
					_currentIndex--;
				}
			}

			undoable.Do();
		}

		public bool Undo()
		{
			if (_currentIndex < 0)
			{
				return false;
			}

			IUndoable undoable = _undoables[_currentIndex--];
			undoable.Undo();

			Undone?.Invoke();

			return true;
		}

		public bool Redo()
		{
			if (_currentIndex >= _undoables.Count - 1)
			{
				return false;
			}

			IUndoable undoable = _undoables[++_currentIndex];
			undoable.Do();

			Redone?.Invoke();

			return true;
		}

		public void Reset()
		{
			_undoables = HasLimit ? new List<IUndoable>(Limit) : new List<IUndoable>();
			_currentIndex = -1;
		}

		#endregion
	}
}