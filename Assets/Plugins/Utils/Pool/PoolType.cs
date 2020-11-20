namespace Utils.Pool
{
    enum PoolType
    {
        // The pool will auto generate new objects if it is empty. Since this type requires all pooled items to be 
        // identical order of retrieval is unimportant
        EXPANDABLE,
        // The pool will retrieve objects in a random order. Once empty it will throw an exception.
        RANDOM,
        // The pool will retrieve object in a queue. Once empty it will throw an exception. 
        NORMAL
    };
}