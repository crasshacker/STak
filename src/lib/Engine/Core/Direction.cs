using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace STak.TakEngine
{
    /// <summary>
    /// Represents a direction (north/south/east/west), for example as would be associated
    /// with a StackMove.
    /// </summary>
    [Serializable]
    public struct Direction
    {
                                                         // 'N', 'E', 'S', 'W'
        private static readonly char[] c_notations = { 'X', '+', '>', '-', '<' };

        public static readonly Direction None  = new Direction(c_notations[0]);
        public static readonly Direction North = new Direction(c_notations[1]);
        public static readonly Direction East  = new Direction(c_notations[2]);
        public static readonly Direction South = new Direction(c_notations[3]);
        public static readonly Direction West  = new Direction(c_notations[4]);

        public int Value { get; private set; }

        public char Notation => c_notations[Value];


        public Direction(char dir)
        {
            int value = Array.FindIndex(c_notations, x => x == dir);

            if (value == -1)
            {
                throw new Exception($"Invalid direction: {dir}.");
            }

            Value = value;
        }


        public string GetNotation()
        {
            return c_notations[Value].ToString();
        }


        public static Direction[] All
        {
            get
            {
                return new Direction[] { Direction.North, Direction.East, Direction.South, Direction.West };
            }
        }


        public static Direction Reverse(Direction dir)
        {
            if (dir.Value == Direction.North) return Direction.South;
            if (dir.Value == Direction.East ) return Direction.West;
            if (dir.Value == Direction.South) return Direction.North;
            if (dir.Value == Direction.West ) return Direction.East;

            return Direction.None;
        }


        public static implicit operator int(Direction dir)
        {
            return dir.Value;
        }


        public static implicit operator char(Direction dir)
        {
            return dir.Notation;
        }


        public static bool operator ==(Direction dir1, Direction dir2)
        {
            return dir1.Value == dir2.Value;
        }


        public static bool operator !=(Direction dir1, Direction dir2)
        {
            return dir1.Value != dir2.Value;
        }


        public override bool Equals(object obj)
        {
            bool isEqual = false;

            if (obj is Direction dir)
            {
                isEqual = dir.Value == Value;
            }

            return isEqual;
        }


        public override int GetHashCode()
        {
            return Value;
        }


        public override string ToString()
        {
            if      (Value == Direction.None ) return "None";
            else if (Value == Direction.South) return "South";
            else if (Value == Direction.West ) return "West";
            else if (Value == Direction.North) return "North";
            else if (Value == Direction.East ) return "East";
            else                               return "Unknown";
        }
    }
}
