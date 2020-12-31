using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UndoSystem
{
    public class UndoSystem : MonoBehaviour, IUndoSystem
    {
        [SerializeField, Min(0)] private int undoLimit;

        public int Count => _undoables.Count;
        public int Limit { get; private set; }
        
        public event Action Undone;
        public event Action Redone;

        private int _currentIndex;
        private List<IUndoable> _undoables;

        private Queue<IUndoable> _undosThisFrame;
        private Coroutine _groupUndosRoutine;

        #region Unity

        private void Awake()
        {
            Reset();
        }

        #endregion

        #region API

        public void Add(IUndoable undoable)
        {
            _undosThisFrame.Enqueue(undoable);

            if (_groupUndosRoutine == null)
            {
                _groupUndosRoutine = StartCoroutine(GroupUndosAtFrameEndRoutine());
            }
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
            if (undoLimit > 0)
            {
                _undoables = new List<IUndoable>(undoLimit);
                Limit = undoLimit;
            }
            else
            {
                _undoables = new List<IUndoable>();
                Limit = int.MaxValue;
            }

            _currentIndex = -1;

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

                Add_Internal(undoable);
            }

            _groupUndosRoutine = null;
        }

        #endregion

        #region Methods

        private void Add_Internal(IUndoable undoable)
        {
            while (_undoables.Count - 1 > _currentIndex)
            {
                _undoables.RemoveAt(_undoables.Count - 1);
            }

            _undoables.Add(undoable);
            _currentIndex++;

            while (_undoables.Count > Limit)
            {
                _undoables.RemoveAt(0);
                _currentIndex--;
            }
        }

        #endregion
    }
}
