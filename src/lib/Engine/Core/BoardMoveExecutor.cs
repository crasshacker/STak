using System;

namespace STak.TakEngine
{
    /// <summary>
    /// Executes or undoes a move against a Board, and returns the resulting board.
    /// </summary>
    /// <remarks>
    /// Unlike the BitBoardMoveExecutor, this move executor updates the StoneMove or StackMove fully, so that
    /// after a call to MakeMove or RedoMove the move is marked as HasExecuted, while after a call to UndoMove
    /// the HasExecuted flag is reset.
    /// </remarks>
    internal class BoardMoveExecutor : IMoveExecutor<Board>
    {
        internal BoardMoveExecutor()
        {
        }


        public Board MakeMove(Board board, IMove move)
        {
            move.Execute(board);
            return board;
        }


        public Board UndoMove(Board board, IMove move)
        {
            move.Undo(board);
            return board;
        }


        public Board RedoMove(Board board, IMove move)
        {
            move.Redo(board);
            return board;
        }
    }
}
