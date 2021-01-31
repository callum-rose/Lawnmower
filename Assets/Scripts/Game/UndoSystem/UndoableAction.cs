namespace Game.UndoSystem
{
    public delegate void UndoableAction(Xor inverted);
    public delegate void UndoableAction<in T>(T obj, Xor inverted);
    public delegate void UndoableAction<in T1, in T2>(T1 obj1, T2 obj2, Xor inverted);
    // etc
}