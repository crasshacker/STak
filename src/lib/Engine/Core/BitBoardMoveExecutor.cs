using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace STak.TakEngine
{
    /// <summary>
    /// Applies (or unapplies) a move to a BitBoard, and returns the resulting board.
    /// </summary>
    /// <remarks>
    /// Unlike the BoardMoveExecutor, this move executor does not internally update the StoneMove or StackMove
    /// instance passed to the Make/Undo/RedoMove methods, except that it sets the FlattenedStone when a stone
    /// is flattened by a capstone.  That is, it generates the bitboard resulting from the move but does not
    /// mark the move itself as HasExecuted;
    /// </remarks>
    internal class BitBoardMoveExecutor : IMoveExecutor<BitBoard>
    {
        //
        // Either of two algorithms can be used to maintain state.  Performance-wise the two algorithms operate
        // at rougly the same speed, but the ApplyMoveUndos algorithm uses only a small constant amount of space
        // while CloneAndRevert requires a copy of the bitboard for every move made that hasn't yet been undone.
        //
        internal enum Algorithm
        {
            CloneAndRevert,     // Move: Clone board, apply move.  Undo: Revert to saved clone.
            ApplyUndoMoves      // Move: Apply move.               Undo: Unapply move.
        }

        private Algorithm      m_algorithm;
        private List<BitBoard> m_bitBoards;


        internal BitBoardMoveExecutor(Algorithm algorithm = Algorithm.ApplyUndoMoves)
        {
            m_algorithm = algorithm;
            m_bitBoards = algorithm == Algorithm.CloneAndRevert ? new List<BitBoard>() : null;
        }


        public BitBoard MakeMove(BitBoard board, IMove move)
        {
            ValidateBitBoardOperation(board, move);

            if (m_algorithm == Algorithm.CloneAndRevert)
            {
                m_bitBoards.Add(board.Clone());
            }

            board.ApplyMove(move);
            return board;
        }


        public BitBoard UndoMove(BitBoard board, IMove move)
        {
            ValidateBitBoardOperation(board, move, true);

            if (m_algorithm == Algorithm.CloneAndRevert)
            {
                board = m_bitBoards[^1];
                m_bitBoards.RemoveAt(m_bitBoards.Count-1);
            }
            else
            {
                board.UnapplyMove(move);
            }
            return board;
        }


        public BitBoard RedoMove(BitBoard board, IMove move)
        {
            ValidateBitBoardOperation(board, move);

            MakeMove(board, move);
            return board;
        }


        [Conditional("DEBUG_BITBOARD_EVERY_MOVE")]
        private void ValidateBitBoardOperation(BitBoard board, IMove move, bool undo = false)
        {
            var clone = board.Clone();

            if (undo)
            {
                clone.UnapplyMove(move);
                clone.ApplyMove(move);
            }
            else
            {
                clone.ApplyMove(move);
                clone.UnapplyMove(move);
            }

            if (! clone.Equals(board))
            {
                throw new Exception("Bitboard is not in its original state after a move followed by an undo.");
            }
        }
    }
}
