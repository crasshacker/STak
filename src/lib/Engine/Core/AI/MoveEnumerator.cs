using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public static class MoveEnumerator
    {
        public static List<IMove> EnumerateMoves(IBoard board, int turn, int playerId, PlayerReserve reserve,
                                                                                      IList<Cell> boardCells)
        {
            List<IMove> moves = new List<IMove>();

            if (turn == 1)
            {
                foreach (Cell cell in boardCells)
                {
                    if (board[cell].IsEmpty)
                    {
                        moves.Add(new StoneMove(cell, new Stone(playerId, StoneType.Flat)));
                    }
                }
            }
            else
            {
                foreach (Cell cell in boardCells)
                {
                    if (board[cell].IsEmpty)
                    {
                        if (reserve.GetAvailableStoneCount(StoneType.Flat) > 0)
                        {
                            moves.Add(new StoneMove(cell, new Stone(playerId, StoneType.Flat)));
                            moves.Add(new StoneMove(cell, new Stone(playerId, StoneType.Standing)));
                        }
                        if (reserve.GetAvailableStoneCount(StoneType.Cap) > 0)
                        {
                            moves.Add(new StoneMove(cell, new Stone(playerId, StoneType.Cap)));
                        }
                    }
                }

                List<int> dropCounts = new List<int>();

                foreach (Cell cell in boardCells)
                {
                    Stack stack = board[cell];

                    if (!stack.IsEmpty && stack.TopStone.PlayerId == playerId)
                    {
                        for (int takeCount = 1; takeCount <= Math.Min(stack.Count, board.Size); takeCount++)
                        {
                            foreach (Direction direction in Direction.All)
                            {
                                dropCounts.Clear();
                                EnumerateStackMoves(board, cell, cell, direction, takeCount, dropCounts, ref moves);
                            }
                        }
                    }
                }
            }

            return moves;
        }


        private static bool EnumerateStackMoves(IBoard board, Cell startingCell, Cell currentCell, Direction direction,
                                                           int stoneCount, List<int> dropCounts, ref List<IMove> moves)
        {
            currentCell = currentCell.Move(direction);

            if (currentCell.File < 0 || currentCell.File >= board.Size
             || currentCell.Rank < 0 || currentCell.Rank >= board.Size)
            {
                return false;
            }

            Stack startingStack = board[startingCell];
            Stack targetStack   = board[currentCell];

            if (! targetStack.IsEmpty && targetStack.TopStone.Type == StoneType.Standing)
            {
                if (startingStack.TopStone.Type != StoneType.Cap || stoneCount != 1)
                {
                    return false;
                }
            }
            if (! targetStack.IsEmpty && targetStack.TopStone.Type == StoneType.Cap)
            {
                return false;
            }

            dropCounts.Add(0);
            for (int dropCount = stoneCount; dropCount > 0; --dropCount)
            {
                dropCounts[^1] = dropCount;

                if (dropCount == stoneCount)
                {
                    moves.Add(new StackMove(startingCell, dropCounts.Sum(), direction, dropCounts));
                }
                else if (! EnumerateStackMoves(board, startingCell, currentCell, direction, stoneCount-dropCount,
                                                                                          dropCounts, ref moves))
                {
                    break;
                }
            }
            dropCounts.RemoveAt(dropCounts.Count-1);

            return true;
        }
    }
}
