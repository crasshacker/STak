using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace STak.TakEngine
{
    /// <summary>
    /// Represents the current state of the game board.
    /// </summary>
    /// <remarks>
    /// A Tak game uses a Board to maintain the state of stones that have been put into play.  The Board
    /// keeps track of the location of each stone that has been played, and unlike the BitBoard it retains
    /// stone identity.  The Board itself contains no real logic; game logic is encapsulated in the BasicGame
    /// (and Game) and StoneMove and StackMove classes.
    /// </remarks>
    public class Board : IBoard
    {
        private readonly Stack[,] m_stacks;

        public const           int   DefaultSize = 5;
        public static readonly int[] Sizes       = { 3, 4, 5, 6, 7, 8 };

        public int Size { get; }


        public Board(int size)
        {
            Size = size;
            m_stacks = new Stack[size, size];

            for (int file = 0; file < size; ++file)
            {
                for (int rank = 0; rank < size; ++rank)
                {
                    Cell cell = new Cell(file, rank);
                    m_stacks[file, rank] = new Stack(cell);
                }
            }
        }


        public Board Clone()
        {
            Board board = new Board(Size);

            for (int file = 0; file < Size; ++file)
            {
                for (int rank = 0; rank < Size; ++rank)
                {
                    Cell cell = new Cell(file, rank);
                    Stack stack = new Stack(cell);
                    board[file, rank] = stack;

                    foreach (Stone stone in m_stacks[file, rank].Stones)
                    {
                        stack.Stones.Add(stone.Clone());
                    }
                }
            }

            return board;
        }


        public Stack this[Cell cell]
        {
                    get => m_stacks[cell.File, cell.Rank];
            private set => m_stacks[cell.File, cell.Rank] = value;
        }


        public Stack this[int file, int rank]
        {
                    get => m_stacks[file, rank];
            private set => m_stacks[file, rank] = value;
        }


        public IEnumerable<Stack> Stacks
        {
            get
            {
                for (int file = 0; file < Size; ++file)
                {
                    for (int rank = 0; rank < Size; ++rank)
                    {
                        yield return m_stacks[file, rank];
                    }
                }
                yield break;
            }
        }


        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            for (int file = 0; file < Size; ++file)
            {
                for (int rank = 0; rank < Size; ++rank)
                {
                    builder.Append(m_stacks[file, rank].Stones.Count);
                    if (rank < Size-1)
                    {
                        builder.Append(", ");
                    }
                    else
                    {
                        builder.Append("\n");
                    }
                }
            }

            return builder.ToString();
        }


        public static int GetFlatStoneCount(int boardSize)
        {
            return boardSize switch
            {
                3 => 10,
                4 => 15,
                5 => 21,
                6 => 30,
                7 => 40,
                8 => 50,

                _ => throw new ArgumentException($"Invalid board size: {boardSize}.", nameof(boardSize)),
            };
        }


        public static int GetCapstoneCount(int boardSize)
        {
            return boardSize switch
            {
                3 => 0,
                4 => 0,
                5 => 1,
                6 => 1,
                7 => 2,
                8 => 2,

                _ => throw new ArgumentException($"Invalid board size: {boardSize}.", nameof(boardSize)),
            };
        }


        public override bool Equals(object obj)
        {
            bool equals = false;

            if (obj != null && GetType() == obj.GetType())
            {
                Board board = obj as Board;

                if (Size == board.Size)
                {
                    equals = true;

                    for (int file = 0; file < Size; ++file)
                    {
                        for (int rank = 0; rank < Size; ++rank)
                        {
                            Stack thisStack =       m_stacks[file, rank];
                            Stack thatStack = board.m_stacks[file, rank];

                            if (thisStack.Stones.Count != thatStack.Stones.Count)
                            {
                                equals = false;
                            }
                            else
                            {
                                for (int i = 0; i < thisStack.Stones.Count; ++i)
                                {
                                    if (! thisStack[i].Equals(thatStack[i]))
                                    {
                                        equals = false;
                                        break;
                                    }
                                }
                            }
                            if (! equals)
                            {
                                break;
                            }
                        }
                        if (! equals)
                        {
                            break;
                        }
                    }
                }
            }

            return equals;
        }


        public override int GetHashCode()
        {
            int hashCode = Size * 17;

            foreach (var stack in Stacks)
            {
                hashCode = hashCode * 23 + stack.GetHashCode();
            }

            return hashCode;
        }


        internal int GetEmptyCellCount()
        {
            return Stacks.Where(s => s.Count == 0).ToList().Count;
        }



        internal static List<Cell> GetNeighboringCells(IBoard board, Cell cell, Direction direction)
        {
            List<Cell> neighbors = new List<Cell>();

            Direction[] directions = (direction == Direction.None) ? Direction.All
                                                  : new Direction[] { direction };

            foreach (Direction checkDirection in directions)
            {
                Cell neighbor = cell.Move(checkDirection);

                if (neighbor.File >= 0 && neighbor.File < board.Size &&
                    neighbor.Rank >= 0 && neighbor.Rank < board.Size)
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }
    }
}
