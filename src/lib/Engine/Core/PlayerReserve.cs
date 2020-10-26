using System;
using System.Diagnostics;

namespace STak.TakEngine
{
    public class PlayerReserve
    {
        public Reserve FlatStoneReserve { get; init; }
        public Reserve CapstoneReserve  { get; init; }


        private PlayerReserve()
        {
        }


        public PlayerReserve(int boardSize, Player player)
        {
            FlatStoneReserve = new Reserve(boardSize, player, StoneType.Flat);
            CapstoneReserve  = new Reserve(boardSize, player, StoneType.Cap);
        }


        public PlayerReserve Clone()
        {
            PlayerReserve playerReserve = new PlayerReserve
            {
                FlatStoneReserve = FlatStoneReserve.Clone(),
                CapstoneReserve  = CapstoneReserve .Clone()
            };
            return playerReserve;
        }


        public Reserve GetReserveForStoneType(StoneType stoneType)
        {
            return (stoneType == StoneType.Flat || stoneType == StoneType.Standing) ? FlatStoneReserve
                                                : (stoneType == StoneType.Cap) ? CapstoneReserve : null;
        }


        public Stone DrawStone(StoneType stoneType, int stoneId = -1)
        {
            Stone stone;

            if (stoneType == StoneType.Flat || stoneType == StoneType.Standing)
            {
                stone = FlatStoneReserve.DrawStone(stoneId);

                if (stoneType == StoneType.Standing)
                {
                    stone.Type = StoneType.Standing;
                }
            }
            else
            {
                stone = CapstoneReserve.DrawStone(stoneId);
            }

            return stone;
        }


        public void ReturnStone(Stone stone)
        {
            if (stone.Type == StoneType.Flat || stone.Type == StoneType.Standing)
            {
                FlatStoneReserve.ReturnStone(new Stone(stone.PlayerId, StoneType.Flat, stone.Id));
            }
            else
            {
                CapstoneReserve.ReturnStone(new Stone(stone.PlayerId, stone.Type, stone.Id));
            }
        }


        public int GetAvailableStoneCount(StoneType stoneType)
        {
            int count = 0;

            if (stoneType == StoneType.Flat || stoneType == StoneType.Standing)
            {
                count = FlatStoneReserve.AvailableStoneCount;
            }
            else if (stoneType == StoneType.Cap)
            {
                count = CapstoneReserve.AvailableStoneCount;
            }
            else if (stoneType == StoneType.None)
            {
                count = FlatStoneReserve.AvailableStoneCount
                      +  CapstoneReserve.AvailableStoneCount;
            }

            return count;
        }
    }
}
