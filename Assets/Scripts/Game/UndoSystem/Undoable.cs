using System;

namespace Game.UndoSystem
{
    internal class Undoable : IUndoable
    {
        private readonly Action _doAction;
        private readonly Action _undoAction;

        public Undoable(Action doAction, Action undoAction)
        {
            _doAction = doAction;
            _undoAction = undoAction;
        }

        public void Do()
        {
            _doAction.Invoke();
        }

        public void Undo()
        {
            _undoAction.Invoke();
        }
    }
}
