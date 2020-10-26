using System;
using System.Linq;
using Akka.Actor;
using STak.TakEngine;
using STak.TakHub.Interop;
using STak.TakHub.Interop.Dto;

namespace STak.TakEngine.Actors
{
    // *** GAME ACTOR MESSAGE CLASSES ***

    [Serializable] public class GameActorMessage { }

    // Property Getter method classes.

    [Serializable] public class GetGameIdMessage        : GameActorMessage { }
    [Serializable] public class GetGamePrototypeMessage : GameActorMessage { }
    [Serializable] public class GetBoardMessage         : GameActorMessage { }
    [Serializable] public class GetBitBoardMessage      : GameActorMessage { }
    [Serializable] public class GetExecutedMovesMessage : GameActorMessage { }
    [Serializable] public class GetRevertedMovesMessage : GameActorMessage { }
    [Serializable] public class GetPlayersMessage       : GameActorMessage { }
    [Serializable] public class GetReservesMessage      : GameActorMessage { }
    [Serializable] public class GetActivePlayerMessage  : GameActorMessage { }
    [Serializable] public class GetResultMessage        : GameActorMessage { }

    [Serializable] public class GetPlayerOneMessage     : GameActorMessage { }
    [Serializable] public class GetPlayerTwoMessage     : GameActorMessage { }
    [Serializable] public class GetLastPlayerMessage    : GameActorMessage { }
    [Serializable] public class GetActiveReserveMessage : GameActorMessage { }
    [Serializable] public class GetLastReserveMessage   : GameActorMessage { }
    [Serializable] public class GetActivePlyMessage     : GameActorMessage { }
    [Serializable] public class GetActiveTurnMessage    : GameActorMessage { }
    [Serializable] public class GetLastTurnMessage      : GameActorMessage { }
    [Serializable] public class GetLastMoveMessage      : GameActorMessage { }

    [Serializable] public class GetStoneMoveMessage     : GameActorMessage { }
    [Serializable] public class GetStackMoveMessage     : GameActorMessage { }
    [Serializable] public class GetDrawnStoneMessage    : GameActorMessage { }
    [Serializable] public class GetGrabbedStackMessage  : GameActorMessage { }
    [Serializable] public class IsStoneMovingMessage    : GameActorMessage { }
    [Serializable] public class IsStackMovingMessage    : GameActorMessage { }
    [Serializable] public class IsMoveInProgressMessage : GameActorMessage { }

    [Serializable] public class IsInitializedMessage    : GameActorMessage { }
    [Serializable] public class IsStartedMessage        : GameActorMessage { }
    [Serializable] public class IsInProgressMessage     : GameActorMessage { }
    [Serializable] public class IsCompletedMessage      : GameActorMessage { }
    [Serializable] public class WasCompletedMessage     : GameActorMessage { }

    [Serializable] public class InitializeMessage       : GameActorMessage { }
    [Serializable] public class StartMessage            : GameActorMessage { }

    // Method method classes.

    [Serializable]
    public class ChangePlayerMessage : GameActorMessage
    {
        public Player Player { get; set; }

        public ChangePlayerMessage()
        {
        }

        public ChangePlayerMessage(Player player)
        {
            Player = player;
        }
    }


    [Serializable]
    public class HumanizePlayerMessage : GameActorMessage
    {
        public int    PlayerId { get; set; }
        public string Name     { get; set; }

        public HumanizePlayerMessage()
        {
        }

        public HumanizePlayerMessage(int playerId, string name)
        {
            PlayerId = playerId;
            Name     = name;
        }
    }


    [Serializable]
    public class CanMakeMoveMessage : GameActorMessage
    {
        public int   PlayerId  { get; set; }
        public MoveDto MoveDto { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public CanMakeMoveMessage()
        {
        }

        public CanMakeMoveMessage(int playerId, IMove move)
        {
            PlayerId = playerId;
            MoveDto  = Mapper.Map<MoveDto>(move);
        }
    }


    [Serializable]
    public class CanUndoMoveMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public CanUndoMoveMessage()
        {
        }

        public CanUndoMoveMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class CanRedoMoveMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public CanRedoMoveMessage()
        {
        }

        public CanRedoMoveMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class UndoMoveMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public UndoMoveMessage()
        {
        }

        public UndoMoveMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class RedoMoveMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public RedoMoveMessage()
        {
        }

        public RedoMoveMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class MakeMoveMessage : GameActorMessage
    {
        public int   PlayerId  { get; set; }
        public MoveDto MoveDto { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public MakeMoveMessage()
        {
        }

        public MakeMoveMessage(int playerId, IMove move)
        {
            PlayerId = playerId;
            MoveDto  = Mapper.Map<MoveDto>(move);
        }
    }


    [Serializable]
    public class CanDrawStoneMessage : GameActorMessage
    {
        public int       PlayerId  { get; set; }
        public StoneType StoneType { get; set; }

        public CanDrawStoneMessage()
        {
        }

        public CanDrawStoneMessage(int playerId, StoneType stoneType)
        {
            PlayerId  = playerId;
            StoneType = stoneType;
        }
    }


    [Serializable]
    public class CanReturnStoneMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public CanReturnStoneMessage()
        {
        }

        public CanReturnStoneMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class CanPlaceStoneMessage : GameActorMessage
    {
        public int     PlayerId { get; set; }
        public CellDto CellDto  { get; set; }

        public Cell Cell => Mapper.Map<Cell>(CellDto);

        public CanPlaceStoneMessage()
        {
        }

        public CanPlaceStoneMessage(int playerId, Cell cell)
        {
            PlayerId = playerId;
            CellDto  = Mapper.Map<CellDto>(cell);
        }
    }


    [Serializable]
    public class CanGrabStackMessage : GameActorMessage
    {
        public int     PlayerId   { get; set; }
        public CellDto CellDto    { get; set; }
        public int     StoneCount { get; set; }

        public Cell Cell => Mapper.Map<Cell>(CellDto);

        public CanGrabStackMessage()
        {
        }

        public CanGrabStackMessage(int playerId, Cell cell, int stoneCount)
        {
            PlayerId   = playerId;
            CellDto    = Mapper.Map<CellDto>(cell);
            StoneCount = stoneCount;
        }
    }


    [Serializable]
    public class CanDropStackMessage : GameActorMessage
    {
        public int     PlayerId   { get; set; }
        public CellDto CellDto    { get; set; }
        public int     StoneCount { get; set; }

        public Cell Cell => Mapper.Map<Cell>(CellDto);

        public CanDropStackMessage()
        {
        }

        public CanDropStackMessage(int playerId, Cell cell, int stoneCount)
        {
            PlayerId   = playerId;
            CellDto    = Mapper.Map<CellDto>(cell);
            StoneCount = stoneCount;
        }
    }


    [Serializable]
    public class CanAbortMoveMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public CanAbortMoveMessage()
        {
        }

        public CanAbortMoveMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class DrawStoneMessage : GameActorMessage
    {
        public int       PlayerId  { get; set; }
        public StoneType StoneType { get; set; }
        public int       StoneId   { get; set; }

        public DrawStoneMessage()
        {
        }

        public DrawStoneMessage(int playerId, StoneType stoneType, int stoneId)
        {
            PlayerId  = playerId;
            StoneType = stoneType;
            StoneId   = stoneId;
        }
    }


    [Serializable]
    public class ReturnStoneMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public ReturnStoneMessage()
        {
        }

        public ReturnStoneMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class PlaceStoneMessage : GameActorMessage
    {
        public int       PlayerId  { get; set; }
        public CellDto   CellDto   { get; set; }
        public StoneType StoneType { get; set; }

        public Cell Cell => Mapper.Map<Cell>(CellDto);

        public PlaceStoneMessage()
        {
        }

        public PlaceStoneMessage(int playerId, Cell cell, StoneType stoneType)
        {
            PlayerId  = playerId;
            CellDto   = Mapper.Map<CellDto>(cell);
            StoneType = stoneType;
        }
    }


    [Serializable]
    public class GrabStackMessage : GameActorMessage
    {
        public int     PlayerId   { get; set; }
        public int     StoneCount { get; set; }
        public CellDto CellDto    { get; set; }

        public Cell Cell => Mapper.Map<Cell>(CellDto);

        public GrabStackMessage()
        {
        }

        public GrabStackMessage(int playerId, Cell cell, int stoneCount)
        {
            PlayerId   = playerId;
            CellDto    = Mapper.Map<CellDto>(cell);
            StoneCount = stoneCount;
        }
    }


    [Serializable]
    public class DropStackMessage : GameActorMessage
    {
        public int     PlayerId   { get; set; }
        public int     StoneCount { get; set; }
        public CellDto CellDto    { get; set; }

        public Cell Cell => Mapper.Map<Cell>(CellDto);

        public DropStackMessage()
        {
        }

        public DropStackMessage(int playerId, Cell cell, int stoneCount)
        {
            PlayerId   = playerId;
            CellDto    = Mapper.Map<CellDto>(cell);
            StoneCount = stoneCount;
        }
    }


    [Serializable]
    public class AbortMoveMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public AbortMoveMessage()
        {
        }

        public AbortMoveMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class InitiateAbortMessage : GameActorMessage
    {
        public int     PlayerId { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     Duration { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public InitiateAbortMessage()
        {
        }

        public InitiateAbortMessage(int playerId, IMove move, int duration)
        {
            PlayerId = playerId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            Duration = duration;
        }
    }


    [Serializable]
    public class CompleteAbortMessage : GameActorMessage
    {
        public int     PlayerId { get; set; }
        public MoveDto MoveDto  { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public CompleteAbortMessage()
        {
        }

        public CompleteAbortMessage(int playerId, IMove move)
        {
            PlayerId = playerId;
            MoveDto  = Mapper.Map<MoveDto>(move);
        }
    }


    [Serializable]
    public class InitiateMoveMessage : GameActorMessage
    {
        public int     PlayerId { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     Duration { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public InitiateMoveMessage()
        {
        }

        public InitiateMoveMessage(int playerId, IMove move, int duration)
        {
            PlayerId = playerId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            Duration = duration;
        }
    }


    [Serializable]
    public class CompleteMoveMessage : GameActorMessage
    {
        public int    PlayerId { get; set; }
        public MoveDto MoveDto { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public CompleteMoveMessage()
        {
        }

        public CompleteMoveMessage(int playerId, IMove move)
        {
            PlayerId = playerId;
            MoveDto  = Mapper.Map<MoveDto>(move);
        }
    }


    [Serializable]
    public class InitiateUndoMessage : GameActorMessage
    {
        public int   PlayerId { get; set; }
        public int   Duration { get; set; }

        public InitiateUndoMessage()
        {
        }

        public InitiateUndoMessage(int playerId, int duration)
        {
            PlayerId = playerId;
            Duration = duration;
        }
    }


    [Serializable]
    public class CompleteUndoMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public CompleteUndoMessage()
        {
        }

        public CompleteUndoMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class InitiateRedoMessage : GameActorMessage
    {
        public int   PlayerId { get; set; }
        public int   Duration { get; set; }

        public InitiateRedoMessage()
        {
        }

        public InitiateRedoMessage(int playerId, int duration)
        {
            PlayerId = playerId;
            Duration = duration;
        }
    }


    [Serializable]
    public class CompleteRedoMessage : GameActorMessage
    {
        public int PlayerId { get; set; }

        public CompleteRedoMessage()
        {
        }

        public CompleteRedoMessage(int playerId)
        {
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class SetCurrentTurnMessage : GameActorMessage
    {
        public int PlayerId { get; set; }
        public int Turn     { get; set; }

        public SetCurrentTurnMessage()
        {
        }

        public SetCurrentTurnMessage(int playerId, int turn)
        {
            PlayerId = playerId;
            Turn     = turn;
        }
    }


    [Serializable]
    public class TrackMoveMessage : GameActorMessage
    {
        public int              PlayerId         { get; set; }
        public BoardPositionDto BoardPositionDto { get; set; }

        public BoardPosition BoardPosition => Mapper.Map<BoardPosition>(BoardPositionDto);

        public TrackMoveMessage()
        {
        }

        public TrackMoveMessage(int playerId, BoardPosition position)
        {
            PlayerId         = playerId;
            BoardPositionDto = Mapper.Map<BoardPositionDto>(position);
        }
    }


    // *** GAME NOTIFIER ACTOR MESSAGE CLASSES ***

    [Serializable]
    public class GameActivityActorMessage { }


    [Serializable]
    public class GameCreatedMessage : GameActivityActorMessage
    {
        public GamePrototypeDto PrototypeDto { get; set; }

        public GamePrototype Prototype => Mapper.Map<GamePrototype>(PrototypeDto);

        public GameCreatedMessage()
        {
        }

        public GameCreatedMessage(GamePrototype prototype)
        {
            PrototypeDto  = Mapper.Map<GamePrototypeDto>(prototype);
        }
    }


    [Serializable]
    public class GameStartedMessage : GameActivityActorMessage
    {
        public Guid GameId { get; set; }

        public GameStartedMessage()
        {
        }

        public GameStartedMessage(Guid gameId)
        {
            GameId = gameId;
        }
    }


    [Serializable]
    public class GameCompletedMessage : GameActivityActorMessage
    {
        public Guid GameId { get; set; }

        public GameCompletedMessage()
        {
        }

        public GameCompletedMessage(Guid gameId)
        {
            GameId = gameId;
        }
    }


    [Serializable]
    public class TurnStartedMessage : GameActivityActorMessage
    {
        public Guid GameId   { get; set; }
        public int  Turn     { get; set; }
        public int  PlayerId { get; set; }

        public TurnStartedMessage()
        {
        }

        public TurnStartedMessage(Guid gameId, int turn, int playerId)
        {
            GameId   = gameId;
            Turn     = turn;
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class TurnCompletedMessage : GameActivityActorMessage
    {
        public Guid GameId   { get; set; }
        public int  Turn     { get; set; }
        public int  PlayerId { get; set; }

        public TurnCompletedMessage()
        {
        }

        public TurnCompletedMessage(Guid gameId, int turn, int playerId)
        {
            GameId   = gameId;
            Turn     = turn;
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StoneDrawnMessage : GameActivityActorMessage
    {
        public Guid         GameId   { get; set; }
        public StoneMoveDto MoveDto  { get; set; }
        public int          PlayerId { get; set; }

        public StoneMove Move => Mapper.Map<StoneMove>(MoveDto);

        public StoneDrawnMessage()
        {
        }

        public StoneDrawnMessage(Guid gameId, StoneMove stoneMove, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<StoneMoveDto>(stoneMove);
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StoneReturnedMessage : GameActivityActorMessage
    {
        public Guid         GameId   { get; set; }
        public StoneMoveDto MoveDto  { get; set; }
        public int          PlayerId { get; set; }

        public StoneMove Move => Mapper.Map<StoneMove>(MoveDto);

        public StoneReturnedMessage()
        {
        }

        public StoneReturnedMessage(Guid gameId, StoneMove stoneMove, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<StoneMoveDto>(stoneMove);
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StonePlacedMessage : GameActivityActorMessage
    {
        public Guid         GameId   { get; set; }
        public StoneMoveDto MoveDto  { get; set; }
        public int          PlayerId { get; set; }

        public StoneMove Move => Mapper.Map<StoneMove>(MoveDto);

        public StonePlacedMessage()
        {
        }

        public StonePlacedMessage(Guid gameId, StoneMove stoneMove, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<StoneMoveDto>(stoneMove);
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StackGrabbedMessage : GameActivityActorMessage
    {
        public Guid         GameId   { get; set; }
        public StackMoveDto MoveDto  { get; set; }
        public int          PlayerId { get; set; }

        public StackMove Move => Mapper.Map<StackMove>(MoveDto);

        public StackGrabbedMessage()
        {
        }

        public StackGrabbedMessage(Guid gameId, StackMove stackMove, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<StackMoveDto>(stackMove);
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StackDroppedMessage : GameActivityActorMessage
    {
        public Guid         GameId   { get; set; }
        public StackMoveDto MoveDto  { get; set; }
        public int          PlayerId { get; set; }

        public StackMove Move => Mapper.Map<StackMove>(MoveDto);

        public StackDroppedMessage()
        {
        }

        public StackDroppedMessage(Guid gameId, StackMove stackMove, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<StackMoveDto>(stackMove);
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class MoveAbortedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public MoveAbortedMessage()
        {
        }

        public MoveAbortedMessage(Guid gameId, IMove move, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class MoveMadeMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public MoveMadeMessage()
        {
        }

        public MoveMadeMessage(Guid gameId, IMove move, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class AbortInitiatedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }
        public int     Duration { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public AbortInitiatedMessage()
        {
        }

        public AbortInitiatedMessage(Guid gameId, IMove move, int playerId, int duration)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
            Duration = duration;
        }
    }

    [Serializable]
    public class AbortCompletedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public AbortCompletedMessage()
        {
        }

        public AbortCompletedMessage(Guid gameId, IMove move, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
        }
    }

    [Serializable]
    public class MoveInitiatedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }
        public int     Duration { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public MoveInitiatedMessage()
        {
        }

        public MoveInitiatedMessage(Guid gameId, IMove move, int playerId, int duration)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
            Duration = duration;
        }
    }

    [Serializable]
    public class MoveCompletedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public MoveCompletedMessage()
        {
        }

        public MoveCompletedMessage(Guid gameId, IMove move, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
        }
    }

    [Serializable]
    public class UndoInitiatedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }
        public int     Duration { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public UndoInitiatedMessage()
        {
        }

        public UndoInitiatedMessage(Guid gameId, IMove move, int playerId, int duration)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
            Duration = duration;
        }
    }

    [Serializable]
    public class UndoCompletedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public UndoCompletedMessage()
        {
        }

        public UndoCompletedMessage(Guid gameId, IMove move, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
        }
    }

    [Serializable]
    public class RedoInitiatedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }
        public int     Duration { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public RedoInitiatedMessage()
        {
        }

        public RedoInitiatedMessage(Guid gameId, IMove move, int playerId, int duration)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
            Duration = duration;
        }
    }

    [Serializable]
    public class RedoCompletedMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public RedoCompletedMessage()
        {
        }

        public RedoCompletedMessage(Guid gameId, IMove move, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
        }
    }

    [Serializable]
    public class MoveCommencingMessage : GameActivityActorMessage
    {
        public Guid    GameId   { get; set; }
        public MoveDto MoveDto  { get; set; }
        public int     PlayerId { get; set; }

        public IMove Move => Mapper.Map<IMove>(MoveDto);

        public MoveCommencingMessage()
        {
        }

        public MoveCommencingMessage(Guid gameId, IMove move, int playerId)
        {
            GameId   = gameId;
            MoveDto  = Mapper.Map<MoveDto>(move);
            PlayerId = playerId;
        }
    }

    [Serializable]
    public class CurrentTurnSetMessage : GameActivityActorMessage
    {
        public Guid       GameId    { get; set; }
        public int        Turn      { get; set; }
        public StoneDto[] StoneDtos { get; set; }

        public Stone[] Stones => StoneDtos.Select(s => Mapper.Map<Stone>(s)).ToArray();

        public CurrentTurnSetMessage()
        {
        }

        public CurrentTurnSetMessage(Guid gameId, int turn, Stone[] stones)
        {
            GameId    = gameId;
            Turn      = turn;
            StoneDtos = stones?.Select(s => Mapper.Map<StoneDto>(s)).ToArray();
        }
    }

    [Serializable]
    public class MoveTrackedMessage : GameActivityActorMessage
    {
        public Guid             GameId      { get; set; }
        public BoardPositionDto PositionDto { get; set; }

        public BoardPosition Position => Mapper.Map<BoardPosition>(PositionDto);

        public MoveTrackedMessage()
        {
        }

        public MoveTrackedMessage(Guid gameId, BoardPosition position)
        {
            GameId      = gameId;
            PositionDto = Mapper.Map<BoardPositionDto>(position);
        }
    }
}
