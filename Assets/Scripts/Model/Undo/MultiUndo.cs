using System.Collections.Generic;

namespace Game.UndoSystem
{
    public class MultiUndo : IUndoable
    {
        private readonly List<IUndoable> _undoables;

        public MultiUndo(params IUndoable[] undoables)
        {
            _undoables = new List<IUndoable>(undoables);
        }

        public void Undo()
        {
            // Reverse order: later steps may depend on state set by earlier ones (B7).
            for (int i = _undoables.Count - 1; i >= 0; i--)
            {
                _undoables[i].Undo();
            }
        }

        public void Do()
        {
            foreach (IUndoable u in _undoables)
            {
                u.Do();
            }
        }
    }
}
