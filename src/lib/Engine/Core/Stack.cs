using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace STak.TakEngine
{
    [Serializable]
    public class Stack
    {
        public Cell        Cell   { get; init; }
        public List<Stone> Stones { get; init; }

        public int   File                 => Cell.File;
        public int   Rank                 => Cell.Rank;
        public int   Count                => Stones.Count;
        public bool  IsEmpty              => Stones.Count == 0;
        public Stone TopStone             => (Stones.Count > 0) ? Stones[^1] : null;
        public Stone this[int index]      => Stones[index];

        public void AddStone(Stone stone) => Stones.Add(stone);
        public bool Contains(Stone stone) => Stones.Contains(stone);


        public Stack()
        {
            Cell   = Cell.None;
            Stones = new List<Stone>();
        }


        public Stack(int file, int rank)
        {
            Cell   = new Cell(file, rank);
            Stones = new List<Stone>();
        }


        public Stack(Cell cell)
        {
            Cell   = cell;
            Stones = new List<Stone>();
        }


        public Stack Clone()
        {
            Stack stack = new Stack(Cell)
            {
                Stones = Stones.Select(s => s.Clone()).ToList()
            };
            return stack;
        }


        //
        // Stacks are equal if the share the same cell and have matching stones, including matching Ids in each
        // stone.  Here we check for maybe being equal; we can't check full stone equality because stones in
        // stacks on the bitboard are simply bits - a stone is either a one or a zero.  So we don't validate
        // Id's unless both are set to something other than -1.  We only compare player Ids and stone types.
        //
        internal bool MaybeEquals(Stack stack)
        {
            bool maybeEquals = false;

            if (stack != null)
            {
                if (Cell.Equals(stack.Cell) && Stones.Count == stack.Stones.Count)
                {
                    maybeEquals = true;

                    for (int i = 0; i < Stones.Count; ++i)
                    {
                        Stone thisStone = Stones[i];
                        Stone thatStone = stack.Stones[i];

                        if (! thisStone.MaybeEquals(thatStone))
                        {
                            maybeEquals = false;
                            break;
                        }
                    }
                }
            }

            return maybeEquals;
        }


	public override bool Equals(object obj)
        {
            if      (obj is null)                return false;
            else if (ReferenceEquals(this, obj)) return true;
            else if (obj.GetType() != GetType()) return false;
            else                                 return Equals(obj as Stack);
	}


        public bool Equals(Stack stack)
        {
            bool isEqual = false;

            if (Cell.Equals(stack.Cell) && Stones.Count == stack.Stones.Count)
            {
                isEqual = true;

                for (int i = 0; i < Stones.Count; ++i)
                {
                    Stone thisStone = Stones[i];
                    Stone thatStone = stack.Stones[i];

                    if (! thisStone.Equals(thatStone))
                    {
                        isEqual = false;
                        break;
                    }
                }
            }

            return isEqual;
        }


        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap.
            {
                int hash = 17;
                hash = hash * 23 + Cell.GetHashCode();
                foreach (var stone in Stones)
                {
                    hash = hash * 23 + stone.GetHashCode();
                }
                return hash;
            }
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Cell.ToString());
            if (Stones.Count > 0)
            {
                sb.Append('\n');
            }
            foreach (Stone stone in Stones)
            {
                sb.Append(String.Format("\n{0}", stone));
            }
            return sb.ToString();
        }


        public static Stack FromString(string str)
        {
            Stack stack = null;

            if (str != null)
            {
                string[] lines = str.Split('\n');
                stack = new Stack(Cell.FromString(lines[0]));
                for (int i = 1; i < lines.Length; ++i)
                {
                    stack.Stones.Add(Stone.FromString(lines[i]));
                }
            }

            return stack;
        }
    }
}
