using System.Collections.Generic;

namespace Game.UndoSystem
{
    public class MultiUndo : IUndoable
    {
        private List<IUndoable> _undoables;

        public MultiUndo(params IUndoable[] undoables)
        {
            _undoables = new List<IUndoable>(undoables);
        }

        public void Undo()
        {
            foreach (var u in _undoables)
            {
                u.Undo();
            }
        }

        public void Do()
        {
            foreach (var u in _undoables)
            {
                u.Do();
            }
        }
    }
}
