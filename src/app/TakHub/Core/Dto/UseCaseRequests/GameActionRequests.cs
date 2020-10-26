using System;
using STak.TakHub.Core.Domain.Entities;
using STak.TakHub.Core.Hubs;
using STak.TakHub.Interop;
using STak.TakEngine;

namespace STak.TakHub.Core.Dto.UseCaseRequests
{
    public class GameActionRequest
    {
        public GameActionRequest(Attendee attendee)
        {
            Attendee = attendee;
        }

        public Attendee Attendee { get; }
    }


    public class SetPropertyRequest: GameActionRequest
    {
        public SetPropertyRequest(Attendee attendee, string name, string value)
            : base(attendee)
        {
            Name  = name;
            Value = value;
        }

        public string Name  { get; }
        public string Value { get; }
    }


    public class RequestActiveInvitesRequest: GameActionRequest
    {
        public RequestActiveInvitesRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class RequestActiveGamesRequest: GameActionRequest
    {
        public RequestActiveGamesRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class InviteGameRequest: GameActionRequest
    {
        public InviteGameRequest(Attendee attendee, GameInvite invite)
            : base(attendee)
        {
            Invite = invite;
        }

        public GameInvite Invite { get; }
    }


    public class KibitzGameRequest: GameActionRequest
    {
        public KibitzGameRequest(Attendee attendee, Guid gameId)
            : base(attendee)
        {
            GameId = gameId;
        }

        public Guid GameId { get; }
    }


    public class AcceptGameRequest: GameActionRequest
    {
        public AcceptGameRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class AcceptInviteRequest: GameActionRequest
    {
        public AcceptInviteRequest(Attendee attendee, Guid inviteId)
            : base(attendee)
        {
            InviteId = inviteId;
        }

        public Guid InviteId { get; }
    }


    public class InitializeGameRequest: GameActionRequest
    {
        public InitializeGameRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class StartGameRequest: GameActionRequest
    {
        public StartGameRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class QuitGameRequest: GameActionRequest
    {
        public QuitGameRequest(Attendee attendee, Guid gameId)
            : base(attendee)
        {
            GameId = gameId;
        }

        public Guid GameId { get; }
    }


    public class ChatRequest: GameActionRequest
    {
        public ChatRequest(Attendee attendee, Guid gameId, string target, string message)
            : base(attendee)
        {
            GameId  = gameId;
            Target  = target;
            Message = message;
        }

        public Guid   GameId  { get; }
        public string Target  { get; }
        public string Message { get; }
    }


    public class DrawStoneRequest: GameActionRequest
    {
        public DrawStoneRequest(Attendee attendee, StoneType stoneType, int stoneId)
            : base(attendee)
        {
            StoneType = stoneType;
            StoneId   = stoneId;
        }

        public StoneType StoneType { get; }
        public int       StoneId   { get; }
    }


    public class ReturnStoneRequest: GameActionRequest
    {
        public ReturnStoneRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class PlaceStoneRequest: GameActionRequest
    {
        public PlaceStoneRequest(Attendee attendee, Cell cell, StoneType stoneType)
            : base(attendee)
        {
            Cell      = cell;
            StoneType = stoneType;
        }

        public Cell      Cell      { get; }
        public StoneType StoneType { get; }
    }


    public class GrabStackRequest: GameActionRequest
    {
        public GrabStackRequest(Attendee attendee, Cell cell, int stoneCount)
            : base(attendee)
        {
            Cell       = cell;
            StoneCount = stoneCount;
        }

        public Cell Cell       { get; }
        public int  StoneCount { get; }
    }


    public class DropStackRequest: GameActionRequest
    {
        public DropStackRequest(Attendee attendee, Cell cell, int stoneCount)
            : base(attendee)
        {
            Cell       = cell;
            StoneCount = stoneCount;
        }

        public Cell Cell       { get; }
        public int  StoneCount { get; }
    }


    public class MakeMoveRequest: GameActionRequest
    {
        public MakeMoveRequest(Attendee attendee, IMove move)
            : base(attendee)
        {
            Move = move;
        }

        public IMove Move { get; }
    }


    public class UndoMoveRequest: GameActionRequest
    {
        public UndoMoveRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class RedoMoveRequest: GameActionRequest
    {
        public RedoMoveRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class AbortMoveRequest: GameActionRequest
    {
        public AbortMoveRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class InitiateAbortRequest: GameActionRequest
    {
        public InitiateAbortRequest(Attendee attendee, IMove move, int duration)
            : base(attendee)
        {
            Move     = move;
            Duration = duration;
        }

        public IMove Move     { get; }
        public int   Duration { get; }
    }


    public class CompleteAbortRequest: GameActionRequest
    {
        public CompleteAbortRequest(Attendee attendee, IMove move)
            : base(attendee)
        {
            Move = move;
        }

        public IMove Move { get; }
    }


    public class InitiateMoveRequest: GameActionRequest
    {
        public InitiateMoveRequest(Attendee attendee, IMove move, int duration)
            : base(attendee)
        {
            Move     = move;
            Duration = duration;
        }

        public IMove Move     { get; }
        public int   Duration { get; }
    }


    public class CompleteMoveRequest: GameActionRequest
    {
        public CompleteMoveRequest(Attendee attendee, IMove move)
            : base(attendee)
        {
            Move = move;
        }

        public IMove Move { get; }
    }


    public class InitiateUndoRequest: GameActionRequest
    {
        public InitiateUndoRequest(Attendee attendee, int duration)
            : base(attendee)
        {
            Duration = duration;
        }

        public int Duration { get; }
    }


    public class CompleteUndoRequest: GameActionRequest
    {
        public CompleteUndoRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class InitiateRedoRequest: GameActionRequest
    {
        public InitiateRedoRequest(Attendee attendee, int duration)
            : base(attendee)
        {
            Duration = duration;
        }

        public int Duration { get; }
    }


    public class CompleteRedoRequest: GameActionRequest
    {
        public CompleteRedoRequest(Attendee attendee)
            : base(attendee)
        {
        }
    }


    public class TrackMoveRequest: GameActionRequest
    {
        public TrackMoveRequest(Attendee attendee, BoardPosition position)
            : base(attendee)
        {
            Position = position;
        }

        public BoardPosition Position { get; }
    }


    public class SetCurrentTurnRequest: GameActionRequest
    {
        public SetCurrentTurnRequest(Attendee attendee, int turn)
            : base(attendee)
        {
            Turn = turn;
        }

        public int Turn { get; }
    }
}
