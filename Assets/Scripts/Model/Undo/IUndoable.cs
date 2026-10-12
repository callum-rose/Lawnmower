namespace Game.UndoSystem
{
    public interface IUndoable
    {
        void Undo();
        void Do();
    }
}
