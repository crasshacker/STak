using System;

namespace STak.TakEngine
{
    [Serializable]
    public class GameCreatedEventArgs : EventArgs
    {
        public GamePrototype Prototype { get; set; }

        public GameCreatedEventArgs(GamePrototype prototype)
        {
            Prototype = prototype;
        }
    }


    [Serializable]
    public class GameStartedEventArgs : EventArgs
    {
        public Guid Id { get; }

        public GameStartedEventArgs(Guid gameId)
        {
            Id = gameId;
        }
    }


    [Serializable]
    public class GameCompletedEventArgs : EventArgs
    {
        public Guid Id { get; }

        public GameCompletedEventArgs(Guid gameId)
        {
            Id = gameId;
        }
    }


    [Serializable]
    public class TurnStartedEventArgs : EventArgs
    {
        public int Turn     { get; set; }
        public int PlayerId { get; set; }

        public TurnStartedEventArgs(int turn, int playerId)
        {
            Turn     = turn;
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class TurnCompletedEventArgs : EventArgs
    {
        public int Turn     { get; set; }
        public int PlayerId { get; set; }

        public TurnCompletedEventArgs(int turn, int playerId)
        {
            Turn     = turn;
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class MoveMadeEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }

        public MoveMadeEventArgs(IMove move, int playerId)
        {
            Move     = move.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class AbortInitiatedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }
        public int   Duration { get; set; }

        public AbortInitiatedEventArgs(IMove move, int playerId, int duration)
        {
            Move     = move.Clone();
            PlayerId = playerId;
            Duration = duration;
        }
    }


    [Serializable]
    public class AbortCompletedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }

        public AbortCompletedEventArgs(IMove move, int playerId)
        {
            Move     = move.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class MoveInitiatedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }
        public int   Duration { get; set; }

        public MoveInitiatedEventArgs(IMove move, int playerId, int duration)
        {
            Move     = move.Clone();
            PlayerId = playerId;
            Duration = duration;
        }
    }


    [Serializable]
    public class MoveCompletedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }

        public MoveCompletedEventArgs(IMove move, int playerId)
        {
            Move     = move.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class UndoInitiatedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }
        public int   Duration { get; set; }

        public UndoInitiatedEventArgs(IMove move, int playerId, int duration)
        {
            Move     = move.Clone();
            PlayerId = playerId;
            Duration = duration;
        }
    }


    [Serializable]
    public class UndoCompletedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }

        public UndoCompletedEventArgs(IMove move, int playerId)
        {
            Move     = move.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class RedoInitiatedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }
        public int   Duration { get; set; }

        public RedoInitiatedEventArgs(IMove move, int playerId, int duration)
        {
            Move     = move.Clone();
            PlayerId = playerId;
            Duration = duration;
        }
    }


    [Serializable]
    public class RedoCompletedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }

        public RedoCompletedEventArgs(IMove move, int playerId)
        {
            Move     = move.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class MoveAbortedEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }

        public MoveAbortedEventArgs(IMove move, int playerId)
        {
            Move     = move.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class MoveCommencingEventArgs : EventArgs
    {
        public IMove Move     { get; set; }
        public int   PlayerId { get; set; }

        public MoveCommencingEventArgs(IMove move, int playerId)
        {
            Move     = move.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StoneDrawnEventArgs : EventArgs
    {
        public Stone Stone    { get; }
        public int   PlayerId { get; set; }

        public StoneDrawnEventArgs(Stone stone, int playerId)
        {
            Stone    = stone.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StonePlacedEventArgs : EventArgs
    {
        public Cell  Cell     { get; }
        public Stone Stone    { get; }
        public int   PlayerId { get; set; }

        public StonePlacedEventArgs(Cell cell, Stone stone, int playerId)
        {
            Cell     = cell;
            Stone    = stone.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StoneReturnedEventArgs : EventArgs
    {
        public Stone Stone    { get; }
        public int   PlayerId { get; set; }

        public StoneReturnedEventArgs(Stone stone, int playerId)
        {
            Stone    = stone.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StackGrabbedEventArgs : EventArgs
    {
        public Stack Stack    { get; }
        public int   PlayerId { get; set; }

        public StackGrabbedEventArgs(Stack stack, int playerId)
        {
            Stack    = stack.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StackDroppedEventArgs : EventArgs
    {
        public IMove Move     { get; }
        public int   PlayerId { get; set; }

        public StackDroppedEventArgs(StackMove stackMove, int playerId)
        {
            Move = stackMove.Clone();
            PlayerId = playerId;
        }
    }


    [Serializable]
    public class StackModifiedEventArgs : EventArgs
    {
        public Stack Stack { get; }

        public StackModifiedEventArgs(Stack stack)
        {
            Stack    = stack.Clone();
        }
    }


    [Serializable]
    public class CurrentTurnSetEventArgs : EventArgs
    {
        public int     Turn   { get; }
        public Stone[] Stones { get; }

        public CurrentTurnSetEventArgs(int turn, Stone[] stones)
        {
            Turn   = turn;
            Stones = stones;
        }
    }


    [Serializable]
    public class MoveTrackedEventArgs : EventArgs
    {
        public BoardPosition Position { get; }

        public MoveTrackedEventArgs(BoardPosition position)
        {
            Position = position;
        }
    }
}
