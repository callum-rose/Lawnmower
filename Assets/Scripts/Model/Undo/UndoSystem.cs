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
		private bool _isBusy;

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
			using GuardScope _ = Guard();

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

			using GuardScope _ = Guard();

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

			using GuardScope _ = Guard();

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

		#region Methods

		/// <summary>
		/// Do/Undo/Redo must never be re-entered from inside an undoable (that was how the spring
		/// bugs B1/B2 hid). Fail loudly instead.
		/// </summary>
		private GuardScope Guard()
		{
			if (_isBusy)
			{
				throw new InvalidOperationException("UndoSystem re-entered while a Do/Undo/Redo was running");
			}

			_isBusy = true;
			return new GuardScope(this);
		}

		private readonly struct GuardScope : IDisposable
		{
			private readonly UndoSystem _owner;

			public GuardScope(UndoSystem owner) => _owner = owner;

			public void Dispose() => _owner._isBusy = false;
		}

		#endregion
	}
}