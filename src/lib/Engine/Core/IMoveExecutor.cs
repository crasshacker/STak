using System;

namespace STak.TakEngine
{
    internal interface IMoveExecutor<T> where T : IBoard
    {
        public T MakeMove(T board, IMove move);
        public T UndoMove(T board, IMove move);
        public T RedoMove(T board, IMove move);
    }
}
