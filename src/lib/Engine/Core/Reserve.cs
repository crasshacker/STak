using System;
using System.Linq;
using System.Collections.Generic;

namespace STak.TakEngine
{
    public interface IReserveStoneSelector
    {
        Stone SelectAccessibleStone(Reserve reserve);
    }


    public class Reserve
    {
        public readonly int BoardSize;

        public List<Stone> Stones    { get; init; }
        public Player      Player    { get; init; }
        public StoneType   StoneType { get; init; }

        public IEnumerable<Stone> AvailableStones     => Stones.Where(s => s != null);
        public int                AvailableStoneCount => AvailableStones.Count();


        public Reserve(int boardSize, Player player, StoneType stoneType)
        {
            Player    = player;
            BoardSize = boardSize;
            StoneType = stoneType;

            int baseIndex  = GetBaseIndex();
            int stoneCount = StartingStoneCount;
            Stones = new List<Stone>(stoneCount);

            for (int i = 0; i < stoneCount; ++i)
            {
                Stones.Add(new Stone(player.Id, stoneType, baseIndex + i));
            }
        }


        public Reserve Clone()
        {
            var reserve = new Reserve(BoardSize, Player, StoneType)
            {
                Stones = Stones.Select(s => s?.Clone()).ToList()
            };
            return reserve;
        }


        public Stone this[int index]
        {
            get
            {
                if (index < 0 || index >= Stones.Count)
                {
                    throw new ArgumentException("Invalid stone index.", nameof(index));
                }

                return Stones[index];
            }
        }


        public int StartingStoneCount
        {
            get
            {
                return StoneType switch
                {
                    StoneType.Standing => Board.GetFlatStoneCount(BoardSize),
                    StoneType.Flat     => Board.GetFlatStoneCount(BoardSize),
                    StoneType.Cap      => Board.GetCapstoneCount(BoardSize),
                    _                  => throw new Exception($"Invalid stone type: {StoneType}.")
                };
            }
        }


        public Stone DrawStone(int stoneId = -1)
        {
            Stone stone = null;

            if (stoneId == -1)
            {
                var selector = new CellAlignedStackReserveStoneSelector();
                stone = selector.SelectAccessibleStone(this);

                if (stone != null)
                {
                    int stoneIndex = stone.Id - GetBaseIndex();
                    Stones[stoneIndex] = null;
                }
            }
            else
            {
                int stoneIndex = stoneId - GetBaseIndex();
                stone = Stones[stoneIndex];
                Stones[stoneIndex] = null;
            }

            return stone;
        }


        public void ReturnStone(Stone stone)
        {
            Stones[stone.Id-GetBaseIndex()] = stone;
        }


        private int GetBaseIndex()
        {
            return Player.Id * (Board.GetFlatStoneCount(BoardSize) + Board.GetCapstoneCount(BoardSize))
                               + (StoneType == StoneType.Flat ? 0 : Board.GetFlatStoneCount(BoardSize));
        }


        private class CellAlignedStackReserveStoneSelector : IReserveStoneSelector
        {
            //
            // This class assumes that stones are laid out in stacks along one edge of the table, with the number of
            // flat stone stacks being equal to the board size minus the number of capstones.  The flat stones are
            // numbered from left to right, bottom to top, so a board of size five would have four stacks of flat
            // stones aligned with the four leftmost board cells, followed by a capstone on the right.  The bottom
            // four flat stones would be numbered 0-3, the next layer 4-7, and so on.  The capstone Id's are the
            // number(s) immediately higher than the highest flat stone number (so on a board of size five, the
            // Id of the sole capstone would be 21.
            //
            // NOTE: The Ids given in the example above assume we're talking about player 1's stones.  For player 2,
            //       the Ids would be equal to these Ids plus the total count of player 1's stones.
            //
            public Stone SelectAccessibleStone(Reserve reserve)
            {
                Stone stone = null;

                for (int stoneIndex = reserve.Stones.Count-1; stoneIndex >= 0; --stoneIndex)
                {
                    if (reserve[stoneIndex] != null)
                    {
                        stone = reserve[stoneIndex];
                        break;
                    }
                }

                return stone;
            }
        }
    }
}
