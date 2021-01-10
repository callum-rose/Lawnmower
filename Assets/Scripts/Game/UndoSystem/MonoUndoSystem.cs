using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UndoSystem
{
	public class MonoUndoSystem : MonoBehaviour, IUndoSystem
	{
		[SerializeField, Min(0)] private int undoLimit = int.MaxValue;

		public int Count => _undoSystem.Count;
		public int Limit => _undoSystem.Limit;

		public event Action Undone
		{
			add => _undoSystem.Undone += value;
			remove => _undoSystem.Undone -= value;
		}

		public event Action Redone
		{
			add => _undoSystem.Redone += value;
			remove => _undoSystem.Redone -= value;
		}

		private IUndoSystem _undoSystem;

		private Queue<IUndoable> _undosThisFrame;
		private Coroutine _groupUndosRoutine;

		#region Unity

		private void Awake()
		{
			_undoSystem = new UndoSystem(undoLimit);

			Reset();
		}

		#endregion

		#region API

		public void Do(IUndoable undoable)
		{
			_undosThisFrame.Enqueue(undoable);

			_groupUndosRoutine ??= StartCoroutine(GroupUndosAtFrameEndRoutine());
		}

		public bool Undo()
		{
			return _undoSystem.Undo();
		}

		public bool Redo()
		{
			return _undoSystem.Redo();
		}

		public void Reset()
		{
			_undoSystem.Reset();
			_undosThisFrame = new Queue<IUndoable>();
		}

		#endregion

		#region Routines

		private IEnumerator GroupUndosAtFrameEndRoutine()
		{
			yield return new WaitForEndOfFrame();

			if (_undosThisFrame.Count > 0)
			{
				IUndoable undoable;
				if (_undosThisFrame.Count > 1)
				{
					undoable = new MultiUndo(_undosThisFrame.ToArray());
					_undosThisFrame.Clear();
				}
				else
				{
					undoable = _undosThisFrame.Dequeue();
				}

				_undoSystem.Do(undoable);
			}

			_groupUndosRoutine = null;
		}

		#endregion
	}
}