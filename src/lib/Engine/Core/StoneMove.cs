using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace STak.TakEngine
{
    [Serializable]
    public class StoneMove : IMove
    {
        public Cell      TargetCell  { get; internal set; }
        public Stone     Stone       { get; internal set; }
        public bool      HasExecuted { get; private  set; }


        public StoneMove()
        {
            TargetCell = Cell.None;
        }


        public StoneMove(Stone stone)
        {
            Stone       = stone;
            TargetCell  = Cell.None;
            HasExecuted = false;
        }


        public StoneMove(Cell cell, Stone stone)
        {
            Stone       = stone;
            TargetCell  = cell;
            HasExecuted = false;
        }


        public StoneMove(int playerId, string ptn)
        {
            StoneMove move = (StoneMove) PtnParser.ParseMove(playerId, ptn, typeof(StoneMove));
            TargetCell     = move.TargetCell;
            HasExecuted    = move.HasExecuted;
            Stone          = move.Stone;
        }


        public IMove Clone()
        {
            StoneMove stoneMove = new StoneMove(TargetCell, Stone.Clone())
            {
                HasExecuted = HasExecuted
            };
            return stoneMove;
        }


        public IEnumerable<Stack> GetAffectedStacks(IBoard board)
        {
            return new List<Stack> { board[TargetCell] };
        }


        public void Execute(IBoard board)
        {
            if (! HasExecuted)
            {
                board[TargetCell].AddStone(Stone);
                HasExecuted = true;
            }
        }


        public void Undo(IBoard board)
        {
            Debug.Assert(board[TargetCell].Count == 1);

            if (HasExecuted)
            {
                board[TargetCell].Stones.RemoveAt(0);
                HasExecuted = false;
            }
        }


        public void Redo(IBoard board)
        {
            board[TargetCell].Stones.Add(Stone);
            HasExecuted = true;
        }


        public override string ToString()
        {
            return ToString(true);
        }


        public string ToString(bool verbose)
        {
            return Stone.GetNotation(verbose) + TargetCell.GetNotation();
        }


	public override bool Equals(object obj)
        {
            if      (ReferenceEquals(null, obj)) return false;
            else if (ReferenceEquals(this, obj)) return true;
            else if (obj.GetType() != GetType()) return false;
            else                                 return Equals(obj as StoneMove);
	}


        public bool Equals(StoneMove move)
        {
            return move != null
                && TargetCell.Equals(move.TargetCell)
                && Stone     .Equals(move.Stone);
        }


        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap.
            {
                int hash = 17;
                hash = hash * 23 + TargetCell.GetHashCode();
                hash = hash * 23 + Stone.GetHashCode();
                return hash;
            }
        }


        [Conditional("DEBUG_MOVE_SERIALIZATION")]
        private void LogMove(bool serializing)
        {
            string intro = serializing ? "Serializing" : "Deserializing";

            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("{0} StoneMove\n", intro));
            sb.Append(String.Format("Move PTN:    {0}\n", ToString()));
            sb.Append(String.Format("Target Cell: {0}\n", TargetCell));
            sb.Append(String.Format("Stone:       {0}\n", Stone));
            sb.Append(String.Format("HasExecuted: {0}\n", HasExecuted));

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
