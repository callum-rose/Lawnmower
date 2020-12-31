using System;
using IUnified;

namespace Game.UndoSystem
{
    public interface IUndoSystem
    {
        int Count { get; }
        int Limit { get; }

        event Action Undone, Redone;
        
        void Add(IUndoable undoable);
        bool Redo();
        bool Undo();
        void Reset();
    }

    [Serializable]
    public class IUndoSystemContainer : IUnifiedContainer<IUndoSystem>
    {

    }
}
