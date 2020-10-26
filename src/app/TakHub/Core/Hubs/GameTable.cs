using System;
using System.Linq;
using System.Collections.Generic;
using STak.TakHub.Interop;
using STak.TakEngine;

namespace STak.TakHub.Core.Hubs
{
    public class GameTable
    {
        public IGame          Game       { get; }
        public HubGameType    GameType   { get; }
        public Attendee       PlayerOne  { get; }
        public Attendee       PlayerTwo  { get; }
        public List<Attendee> Kibitzers  { get; }
        public bool           P1Accepted { get; set; }
        public bool           P2Accepted { get; set; }
        public bool           P1Detached { get; set; }
        public bool           P2Detached { get; set; }
        public string         Host       { get; set; }

        public IEnumerable<Attendee> Players   => new Attendee[] { PlayerOne, PlayerTwo };
        public IEnumerable<Attendee> Attendees => Players.Concat(Kibitzers);

        public string Player1Id => PlayerOne.ConnectionId;
        public string Player2Id => PlayerTwo.ConnectionId;

        public string Id => Game.Id.ToString();


        public GameTable(IGame game, HubGameType gameType, Attendee playerOne, Attendee playerTwo)
        {
            Game         = game;
            GameType     = gameType;
            PlayerOne    = playerOne;
            PlayerTwo    = playerTwo;
            Kibitzers    = new List<Attendee>();
        }


        public bool HasPlayer(Attendee attendee)
        {
            return (attendee == PlayerOne && ! P1Detached)
                || (attendee == PlayerTwo && ! P2Detached);
        }


        public bool HasPlayerForConnection(string connectionId)
        {
            return (PlayerOne.ConnectionId == connectionId && ! P1Detached)
                || (PlayerTwo.ConnectionId == connectionId && ! P2Detached);
        }


        public bool HasPlayerDetached(Attendee attendee)
        {
            return (attendee == PlayerOne && P1Detached)
                || (attendee == PlayerTwo && P2Detached);
        }


        public Attendee GetOpponentOf(Attendee player)
        {
            return (player == PlayerOne) ? PlayerTwo
                 : (player == PlayerTwo) ? PlayerOne
                 : null;
        }


        public void DetachPlayer(Attendee player)
        {
            if      (player == PlayerOne) { P1Detached = true; }
            else if (player == PlayerTwo) { P2Detached = true; }
            else throw new Exception($"Cannot detach \"{player.UserName}\" - not a player in this game.");
        }


        public bool HasKibitzer(Attendee attendee)
        {
            return Kibitzers.Contains(attendee);
        }


        public bool HasAttendee(Attendee attendee)
        {
            return HasPlayer(attendee) || HasKibitzer(attendee);
        }


        public void AddKibitzer(Attendee kibitzer)
        {
            Kibitzers.Add(kibitzer);
        }


        public void RemoveKibitzer(Attendee kibitzer)
        {
            Kibitzers.Remove(kibitzer);
        }
    }
}
