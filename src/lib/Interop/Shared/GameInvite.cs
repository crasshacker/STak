using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NodaTime;
using STak.TakEngine;

namespace STak.TakHub.Interop
{
    [Serializable]
    public class GameInvite
    {
        public Guid               Id           { get; set; }
        public string             Inviter      { get; set; }
        public string             ConnectionId { get; set; }
        public List<int>          PlayerNumber { get; set; }
        public List<int>          BoardSize    { get; set; }
        public List<string>       Opponent     { get; set; }
        public bool               IsInviterAI  { get; set; }
        public bool               WillPlayAI   { get; set; }
        public HubGameType        GameType     { get; set; }
        public Instant            CreateTime   { get; set; }
        public (int Min, int Max) TimeLimit    { get; set; }
        public (int Min, int Max) Increment    { get; set; }


        public GameInvite()
        {
            Id           = Guid.NewGuid();
            PlayerNumber = new List<int>();
            BoardSize    = new List<int>();
            Opponent     = new List<string>();
            IsInviterAI  = false;
            WillPlayAI   = false;
            GameType     = new HubGameType(HubGameType.Any);
            CreateTime   = SystemClock.Instance.GetCurrentInstant();
            TimeLimit    = (Int32.MaxValue, Int32.MaxValue);
            Increment    = (Int32.MaxValue, Int32.MaxValue);
        }


        public GameInvite(IEnumerable<string> opponent, IEnumerable<int> playerNumber, IEnumerable<int> boardSize,
                                                          bool isInviterAI, bool willPlayAI, HubGameType gameType,
                                                                       (int, int) timeLimit, (int, int) increment)
        {
            Id           = Guid.NewGuid();
            CreateTime   = SystemClock.Instance.GetCurrentInstant();
            PlayerNumber = (playerNumber != null) ? new List<int>   (playerNumber) : new List<int>   ();
            BoardSize    = (boardSize    != null) ? new List<int>   (boardSize)    : new List<int>   ();
            Opponent     = (opponent     != null) ? new List<string>(opponent)     : new List<string>();
            IsInviterAI  = isInviterAI;
            WillPlayAI   = willPlayAI;
            GameType     = gameType;
            TimeLimit    = timeLimit;
            Increment    = increment;
        }


        public GameInvite(IEnumerable<string> opponent, IEnumerable<int> playerNumber, IEnumerable<int> boardSize,
                                                          bool isInviterAI, bool willPlayAI, HubGameType gameType)
            : this(opponent, playerNumber, boardSize, isInviterAI, willPlayAI, gameType,
                      (Int32.MaxValue, Int32.MaxValue), (Int32.MaxValue, Int32.MaxValue))
        {
        }


        public GameInvite(GameInvite invite)
        {
            Id           = invite.Id;
            Inviter      = invite.Inviter;
            ConnectionId = invite.ConnectionId;
            PlayerNumber = new List<int>(invite.PlayerNumber);
            BoardSize    = new List<int>(invite.BoardSize);
            Opponent     = new List<string>(invite.Opponent);
            IsInviterAI  = invite.IsInviterAI;
            WillPlayAI   = invite.WillPlayAI;
            GameType     = invite.GameType;
            CreateTime   = invite.CreateTime;
            TimeLimit    = invite.TimeLimit;
            Increment    = invite.Increment;
        }


        public GameInvite Clone()
        {
            return new GameInvite(this);
        }


        public static bool IsMatch(GameInvite invite1, GameInvite invite2)
        {
            return ((invite1.Opponent.Contains(invite2.Inviter) || ! invite1.Opponent.Any())
                &&  (invite2.Opponent.Contains(invite1.Inviter) || ! invite2.Opponent.Any()))

                && ((! invite1.PlayerNumber.Any()) || (! invite2.PlayerNumber.Any())
                ||  (invite1.PlayerNumber.Contains(Player.One) && invite2.PlayerNumber.Contains(Player.Two))
                ||  (invite1.PlayerNumber.Contains(Player.Two) && invite2.PlayerNumber.Contains(Player.One)))

                && (invite1.BoardSize.Intersect(invite2.BoardSize).Any() || ! invite1.BoardSize.Any()
                                                                         || ! invite2.BoardSize.Any())

                && ((invite1.WillPlayAI || ! invite2.IsInviterAI)
                &&  (invite2.WillPlayAI || ! invite1.IsInviterAI))

                && (HubGameType.IsMatch(invite1.GameType, invite2.GameType))

                && ((invite1.TimeLimit.Min <= invite2.TimeLimit.Max && invite1.TimeLimit.Min >= invite2.TimeLimit.Min)
                 || (invite1.TimeLimit.Max <= invite2.TimeLimit.Max && invite1.TimeLimit.Min >= invite2.TimeLimit.Min))

                && ((invite1.Increment.Min <= invite2.Increment.Max && invite1.Increment.Min >= invite2.Increment.Min)
                 || (invite1.Increment.Max <= invite2.Increment.Max && invite1.Increment.Min >= invite2.Increment.Min));
        }
    }
}
