using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace STak.TakEngine
{
    [Serializable]
    public class StackMove : IMove, IEquatable<StackMove>
    {
        public Cell      StartingCell   { get; private set; }
        public Direction Direction      { get; private set; }
        public int       StoneCount     { get; private set; }
        public List<int> DropCounts     { get; private set; }
        public int       LastDropCount  { get; private set; }
        public Stack     GrabbedStack   { get; private set; }
        public Stone     FlattenedStone { get; private set; }
        public bool      HasExecuted    { get; private set; }

        public bool      IsFlattening        => FlattenedStone != null;
        public int       RemainingStoneCount => StoneCount - DropCounts.Sum(c => c);
        public Cell      EndingCell          => StartingCell.Move(Direction, Distance);
        public int       Distance            => DropCounts.Count;


        public StackMove()
        {
        }


        public StackMove(Cell cell, int stoneCount, Direction direction, IEnumerable<int> dropCounts = null,
                                                                                        IBoard board = null)
        {
            if (dropCounts == null)
            {
                dropCounts = Array.Empty<int>();
            }

            StartingCell   = cell;
            Direction      = direction;
            StoneCount     = stoneCount;
            DropCounts     = new List<int>(dropCounts);
            LastDropCount  = (DropCounts.Count > 0) ? DropCounts[^1] : 0;
            HasExecuted    = false;
            FlattenedStone = null;

            if (board != null)
            {
                var firstStone = board[cell].TopStone;
                var finalStone = board[cell.Move(direction, DropCounts.Count)].TopStone;

                if (firstStone.Type == StoneType.Cap && finalStone.Type == StoneType.Standing)
                {
                    Debug.Assert(DropCounts.Count > 0 && DropCounts[^1] == 1);
                    FlattenedStone = finalStone;
                }
            }
        }


        public StackMove(int playerId, string ptn)
        {
            StackMove move = (StackMove) PtnParser.ParseMove(playerId, ptn, typeof(StackMove));
            StartingCell   = move.StartingCell;
            Direction      = move.Direction;
            StoneCount     = move.StoneCount;
            DropCounts     = move.DropCounts;
            LastDropCount  = move.LastDropCount;
            HasExecuted    = move.HasExecuted;
            FlattenedStone = move.FlattenedStone;
        }


        public IMove Clone()
        {
            StackMove stackMove = new StackMove(StartingCell, StoneCount, Direction, DropCounts)
            {
                LastDropCount  = LastDropCount,
                GrabbedStack   = GrabbedStack?.Clone(),
                FlattenedStone = FlattenedStone,
                HasExecuted    = HasExecuted
            };
            return stackMove;
        }


        public Cell CurrentCell
        {
            get
            {
                Cell currentCell = StartingCell;
                for (int i = 0; i < DropCounts.Count; ++i)
                {
                    currentCell = currentCell.Move(Direction);
                }
                return currentCell;
            }
        }


        public bool GrabStack(IBoard board, bool grabStones = true)
        {
            bool didGrabStack = false;

            if (GrabbedStack == null)
            {
                Stack boardStack = board[StartingCell];
                GrabbedStack = new Stack(StartingCell);
                GrabbedStack.Stones.AddRange(boardStack.Stones.Skip(boardStack.Count-StoneCount));
                if (grabStones)
                {
                    boardStack.Stones.RemoveRange(boardStack.Count-StoneCount, StoneCount);
                }
                didGrabStack = true;
            }

            return didGrabStack;
        }


        public bool CanDropStack(IBoard board, Cell cell, int dropCount)
        {
            bool canDrop = false;

            // NOTE: This method assumes GrabStack has already been called and stones have already been
            //       dropped on all stones between the starting cell and the cell targeted by this call.
            if (GrabbedStack == null)
            {
                throw new Exception("You must call GrabStack before calling CanDropStack.");
            }

            int droppedCount = DropCounts.Sum(c => c);

            // Drop is not allowed in starting cell or if not enough stones remain to drop.
            if (cell != StartingCell && droppedCount + dropCount <= StoneCount)
            {
                if (cell == CurrentCell)
                {
                    // We've already dropped stone(s) here, so we can drop more.
                    canDrop = true;
                }
                else
                {
                    if (Board.GetNeighboringCells(board, CurrentCell, Direction).Contains(cell))
                    {
                        canDrop = board[cell].TopStone == null;

                        if (! canDrop)
                        {
                            StoneType stoneType = board[cell].TopStone.Type;
                            canDrop = (stoneType == StoneType.Flat) || (stoneType == StoneType.Standing &&
                                                GrabbedStack.TopStone.Type == StoneType.Cap &&
                                                dropCount == 1 && droppedCount + dropCount == StoneCount);
                        }
                    }
                }
            }

            return canDrop;
        }


        public bool DropStack(IBoard board, Cell cell, int dropCount, bool placeStones = true)
        {
            if (! HasExecuted && CanDropStack(board, cell, dropCount))
            {
                if (Direction == Direction.None)
                {
                    Direction = StartingCell.GetDirectionTo(cell);
                }

                Cell currentCell = CurrentCell;

                if (cell == CurrentCell)
                {
                    DropCounts[^1] += dropCount;
                }
                else
                {
                    DropCounts.Add(dropCount);
                }

                if (GrabbedStack.Stones.Count == 1 && GrabbedStack.TopStone.Type == StoneType.Cap &&
                    board[cell].Stones.Count > 0 && board[cell].TopStone.Type == StoneType.Standing)
                {
                    FlattenedStone = board[cell].TopStone;
                    // Don't flatten the stone associated with the move, since doing so would cause the move
                    // that originally placed the standing stone to have instead placed a flat stone.  Instead
                    // leave the move's FlattenedStone as it is and replace the stone in the stack on the board
                    // with a clone of the stone, and then mark that stone as flat.
                    board[cell].Stones[^1] = board[cell].Stones[^1].Clone();
                    board[cell].TopStone.Type = StoneType.Flat;
                }

                if (placeStones)
                {
                    board[cell].Stones.AddRange(GrabbedStack.Stones.Take(dropCount));
                }
                GrabbedStack.Stones.RemoveRange(0, dropCount);
                LastDropCount = dropCount;

                if (GrabbedStack.TopStone == null)
                {
                    Debug.Assert(DropCounts.Sum(c => c) == StoneCount);
                    HasExecuted = true;
                }
            }

            return HasExecuted;
        }


        public void Execute(IBoard board)
        {
            if (! HasExecuted)
            {
                GrabStack(board);

                Debug.Assert(GrabbedStack.Count == DropCounts.Sum());
                List<int> dropCounts = new List<int>(DropCounts);
                DropCounts.Clear();

                Cell currentCell = StartingCell;
                foreach (int dropCount in dropCounts)
                {
                    currentCell = currentCell.Move(Direction);
                    DropStack(board, currentCell, dropCount);
                }

                HasExecuted = true;
            }
        }


        public void Undo(IBoard board)
        {
            if (HasExecuted)
            {
                Cell cell = StartingCell.Move(Direction);

                for (int i = 0; i < DropCounts.Count; i++)
                {
                    List<Stone> stones = board[cell].Stones;
                    board[StartingCell].Stones.AddRange(stones.Skip(stones.Count-DropCounts[i]));
                    stones.RemoveRange(stones.Count-DropCounts[i], DropCounts[i]);
                    cell = cell.Move(Direction);
                }

                if (FlattenedStone != null)
                {
                    Cell finalCell = StartingCell.Move(Direction, DropCounts.Count);
                    Debug.Assert(board[finalCell].TopStone.Id == FlattenedStone.Id);
                    board[finalCell].Stones[^1] = FlattenedStone;
                    FlattenedStone = null;
                }

                LastDropCount = 0;
                GrabbedStack = null;
                HasExecuted = false;
            }
        }


        public void Redo(IBoard board)
        {
            Reset();
            bool grabbedStack = GrabStack(board);
            Debug.Assert(grabbedStack);

            List<int> dropCounts = DropCounts;
            DropCounts = new List<int>();

            Cell currentCell = StartingCell;
            Cell finalCell   = StartingCell.Move(Direction, dropCounts.Count);
            bool isCompleted = false;

            foreach (int dropCount in dropCounts)
            {
                currentCell = currentCell.Move(Direction);

                if (CanDropStack(board, currentCell, dropCount))
                {
                    isCompleted = DropStack(board, currentCell, dropCount);
                }

                Debug.Assert(isCompleted == (currentCell == finalCell));
            }

            HasExecuted = true;
        }


        public void Abort(IBoard board)
        {
            Cell cell = StartingCell.Move(Direction);

            for (int i = 0; i < DropCounts.Count; i++)
            {
                List<Stone> stones = board[cell].Stones;
                board[StartingCell].Stones.AddRange(stones.Skip(stones.Count-DropCounts[i]));
                stones.RemoveRange(stones.Count-DropCounts[i], DropCounts[i]);
                cell = cell.Move(Direction);
            }

            // Return all stones dropped so far during the move.
            board[StartingCell].Stones.AddRange(GrabbedStack.Stones);

            LastDropCount = 0;
            GrabbedStack = null;
            HasExecuted = false;
        }


        public void Reset()
        {
            GrabbedStack = null;
            HasExecuted = false;
        }


        public void RestoreOriginalGrabbedStack(IBoard board)
        {
            if (GrabbedStack == null)
            {
                GrabbedStack = new Stack(StartingCell);
            }

            var stones = new List<Stone>();
            var cell = StartingCell;

            for (int i = 0; i < Distance; ++i)
            {
                cell = cell.Move(Direction);
                var cellStack = board[cell];
                stones.AddRange(cellStack.Stones.Skip(cellStack.Count-DropCounts[i]));
            }

            GrabbedStack.Stones.InsertRange(0, stones);
        }


        public IEnumerable<Stack> GetAffectedStacks(IBoard board)
        {
            List<Stack> stacks = new List<Stack>();

            Cell currentCell = StartingCell;
            stacks.Add(board[currentCell]);

            if (Direction != Direction.None)
            {
                foreach (int dropCount in DropCounts)
                {
                    currentCell = currentCell.Move(Direction);
                    stacks.Add(board[currentCell]);
                }
            }

            return stacks;
        }


        public override string ToString()
        {
            return ToString(false);
        }


        public string ToString(bool verbose)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append((verbose || StoneCount > 1) ? StoneCount.ToString() : String.Empty);
            builder.Append(StartingCell.GetNotation());
            builder.Append(Direction.GetNotation());

            //
            // The PTN specification doesn't strictly require that drop counts be included if
            // there is only one, because it can be inferred to be equal to the stone count.
            // However, in order to support using PTN notation for partially completed moves
            // (which we currently use to seriaize/deserialize StackMoves/StoneMoves) we need
            // to include all drop counts in effect at this point in the move.  We thus include
            // the stone count in the case of an incomplete move, even if verbose is false.
            //
            if ((StoneCount != DropCounts.Sum()) || (DropCounts.Count > 1) || verbose)
            {
                foreach (int dropCount in DropCounts)
                {
                    builder.Append(dropCount);
                }
            }

            // We'd like to include the type of the top stone here if we're in verbose mode,
            // However, if the move has been executed we no longer have any way of knowing
            // what the top stone was, so we don't include it in this case.
            if (verbose && GrabbedStack?.TopStone != null)
            {
                builder.Append(GrabbedStack.TopStone.GetNotation(true));
            }

            string ptn = builder.ToString();

            return ptn;
        }


	public override bool Equals(object obj)
        {
            if      (obj is null)                return false;
            else if (ReferenceEquals(this, obj)) return true;
            else if (obj.GetType() != GetType()) return false;
            else                                 return Equals(obj as StackMove);
	}


        public bool Equals(StackMove move)
        {
            return move != null
                && StartingCell.Equals(move.StartingCell)
                && Direction   .Equals(move.Direction)
                && StoneCount  .Equals(move.StoneCount)
                && DropCounts  .SequenceEqual(move.DropCounts)
                && Object.Equals(FlattenedStone, move.FlattenedStone)
                && ((GrabbedStack == null && move.GrabbedStack == null)
                             || GrabbedStack.Equals(move.GrabbedStack));
        }


        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap.
            {
                int hash = 17;
                hash = hash * 23 + StartingCell.GetHashCode();
                hash = hash * 23 + Direction.GetHashCode();
                hash = hash * 23 + StoneCount.GetHashCode();
                hash = hash * 23 + DropCounts.Sum(dc => dc.GetHashCode());
                hash = hash * 23 + GrabbedStack.Count.GetHashCode();
                return hash;
            }
        }


        internal void FlattenStone(Stone stone)
        {
            FlattenedStone = stone;
        }


        [Conditional("DEBUG_MOVE_SERIALIZATION")]
        private void LogMove(bool serializing)
        {
            string intro = serializing ? "Serializing" : "Deserializing";

            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("{0} StackMove\n", intro));
            sb.Append(String.Format("Move PTN:      {0}\n", ToString()));
            sb.Append(String.Format("Starting Cell: {0}\n", StartingCell));
            sb.Append(String.Format("Direction:     {0}\n", Direction));
            sb.Append(String.Format("StoneCount:    {0}\n", StoneCount));
            sb.Append(String.Format("DropCounts:    "));
            if (DropCounts.Count == 0)
            {
                sb.Append('\n');
            }
            else
            {
                for (int i = 0; i < DropCounts.Count; ++i)
                {
                    sb.Append(String.Format("{0}", DropCounts[i]));
                    if (i < DropCounts.Count-1) sb.Append(','); else sb.Append('\n');
                }
            }
            sb.Append(String.Format("LastDropCount:  {0}\n", LastDropCount));
            sb.Append(String.Format("HasExecuted:    {0}\n", HasExecuted));
            sb.Append(String.Format("FlattenedStone: {0}\n", FlattenedStone));

            FileStream stream = null;

            while (stream == null)
            {
                string fileName = @"C:\TEMP\TakMove.log";
                try { stream = File.Open(fileName, FileMode.Append, FileAccess.Write, FileShare.None); } catch { }
                if (stream == null) Thread.Sleep(10);
            }

            using StreamWriter writer = new StreamWriter(stream)
            {
                NewLine = "\n"
            };
            writer.Write(sb.ToString() + "\n");
        }
    }
}
