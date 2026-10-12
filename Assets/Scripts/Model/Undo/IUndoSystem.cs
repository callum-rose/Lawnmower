using System;

namespace Game.UndoSystem
{
    public interface IUndoSystem
    {
        int Count { get; }
        int Limit { get; }

        event Action Undone, Redone;
        
        void Do(IUndoable undoable);
        bool Redo();
        bool Undo();
        void Reset();
    }
}
