using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace STak.TakEngine
{
    /// <summary>
    /// Represents a board cell location.
    /// </summary>
    [Serializable]
    public readonly struct Cell
    {
        public static readonly Cell None = new Cell(-1, -1);

        public readonly int File { get; }
        public readonly int Rank { get; }


        public Cell(int file, int rank)
        {
            File = file;
            Rank = rank;
        }


        public Cell Move(Direction dir, int count = 1)
        {
            return Move(this, dir, count);
        }


        public static Cell Move(Cell cell, Direction dir, int count = 1)
        {
            int rank = cell.Rank;
            int file = cell.File;

            if (dir == Direction.North) rank += count; else
            if (dir == Direction.East ) file += count; else
            if (dir == Direction.South) rank -= count; else
            if (dir == Direction.West ) file -= count;

            return new Cell(file, rank);
        }


        public Direction GetDirectionTo(Cell cell)
        {
            Direction direction = Direction.None;

            if (File == cell.File)
            {
                if (Rank != cell.Rank)
                {
                    direction = Rank > cell.Rank ? Direction.South : Direction.North;
                }
            }
            else if (Rank == cell.Rank)
            {
                if (File != cell.File)
                {
                    direction = File > cell.File ? Direction.West : Direction.East;
                }
            }

            return direction;
        }


        public string GetNotation()
        {
            return $"{(char)('a'+File)}{Rank+1}";
        }


        public static int ConvertFile(string fileStr)
        {
            return "abcdefgh".IndexOf(fileStr);
        }


        public static string ConvertFile(int file)
        {
            return "abcdefgh"[file].ToString();
        }


        public static int ConvertRank(string rankStr)
        {
            return Int32.Parse(rankStr) - 1;
        }


        public static string ConvertRank(int rank)
        {
            return (rank+1).ToString();
        }


        public static bool operator==(Cell cell1, Cell cell2)
        {
            return cell1.File == cell2.File &&
                   cell1.Rank == cell2.Rank;
        }


        public static bool operator!=(Cell cell1, Cell cell2)
        {
            return cell1.File != cell2.File ||
                   cell1.Rank != cell2.Rank;
        }


        public override bool Equals(object obj)
        {
            bool isEqual = false;

            if (obj is Cell cell)
            {
                isEqual = cell.File == File && cell.Rank == Rank;
            }

            return isEqual;
        }


        public override int GetHashCode()
        {
            return HashCode.Combine(File, Rank);
        }

        public override string ToString()
        {
            return String.Format("[{0}{1}]", ConvertFile(File), ConvertRank(Rank));
        }


        public static Cell FromString(string str)
        {
            str = str[1..^1];
            string[] fileAndRank = str.Split(',');
            return new Cell(Int32.Parse(fileAndRank[0]), Int32.Parse(fileAndRank[1]));
        }
    }
}
