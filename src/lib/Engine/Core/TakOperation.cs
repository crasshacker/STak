using System;

namespace STak.TakEngine
{
    public abstract class TakOperation
    {
        public string Name         { get; init; }
        public bool   LogExecution { get; init; }
    }


    public abstract class TakCommand : TakOperation
    {
    }


    public abstract class TakNotification : TakOperation
    {
    }


    public class GameCommand : TakCommand
    {
        public static readonly GameCommand DrawStone       = new GameCommand { Name = "DrawStone",       LogExecution = true  };
        public static readonly GameCommand PlaceStone      = new GameCommand { Name = "PlaceStone",      LogExecution = true  };
        public static readonly GameCommand ReturnStone     = new GameCommand { Name = "ReturnStone",     LogExecution = true  };
        public static readonly GameCommand GrabStack       = new GameCommand { Name = "GrabStack",       LogExecution = true  };
        public static readonly GameCommand DropStack       = new GameCommand { Name = "DropStack",       LogExecution = true  };
        public static readonly GameCommand MakeMove        = new GameCommand { Name = "MakeMove",        LogExecution = true  };
        public static readonly GameCommand UndoMove        = new GameCommand { Name = "UndoMove",        LogExecution = true  };
        public static readonly GameCommand RedoMove        = new GameCommand { Name = "RedoMove",        LogExecution = true  };
        public static readonly GameCommand InitiateAbort   = new GameCommand { Name = "InitiateAbort",   LogExecution = true  };
        public static readonly GameCommand CompleteAbort   = new GameCommand { Name = "CompleteAbort",   LogExecution = true  };
        public static readonly GameCommand InitiateMove    = new GameCommand { Name = "InitiateMove",    LogExecution = true  };
        public static readonly GameCommand CompleteMove    = new GameCommand { Name = "CompleteMove",    LogExecution = true  };
        public static readonly GameCommand InitiateUndo    = new GameCommand { Name = "InitiateUndo",    LogExecution = true  };
        public static readonly GameCommand CompleteUndo    = new GameCommand { Name = "CompleteUndo",    LogExecution = true  };
        public static readonly GameCommand InitiateRedo    = new GameCommand { Name = "InitiateRedo",    LogExecution = true  };
        public static readonly GameCommand CompleteRedo    = new GameCommand { Name = "CompleteRedo",    LogExecution = true  };
        public static readonly GameCommand AbortMove       = new GameCommand { Name = "AbortMove",       LogExecution = true  };
        public static readonly GameCommand TrackMove       = new GameCommand { Name = "TrackMove",       LogExecution = false };
        public static readonly GameCommand SetCurrentTurn  = new GameCommand { Name = "SetCurrentTurn",  LogExecution = true  };
        public static readonly GameCommand ChangePlayer    = new GameCommand { Name = "ChangePlayer",    LogExecution = true  };
        public static readonly GameCommand HumanizePlayer  = new GameCommand { Name = "HumanizePlayer",  LogExecution = true  };
        public static readonly GameCommand CancelOperation = new GameCommand { Name = "CancelOperation", LogExecution = true  };
    }


    public class GameNotification : TakNotification
    {
        public static readonly GameNotification GameCreated     = new GameNotification { Name = "GameCreated",       LogExecution = true  };
        public static readonly GameNotification GameStarted     = new GameNotification { Name = "GameStarted",       LogExecution = true  };
        public static readonly GameNotification GameCompleted   = new GameNotification { Name = "GameCompleted",     LogExecution = true  };
        public static readonly GameNotification TurnStarted     = new GameNotification { Name = "TurnStarted",       LogExecution = true  };
        public static readonly GameNotification TurnCompleted   = new GameNotification { Name = "TurnCompleted",     LogExecution = true  };
        public static readonly GameNotification StoneDrawn      = new GameNotification { Name = "StoneDrawn",        LogExecution = true  };
        public static readonly GameNotification StonePlaced     = new GameNotification { Name = "StonePlaced",       LogExecution = true  };
        public static readonly GameNotification StoneReturned   = new GameNotification { Name = "StoneReturned",     LogExecution = true  };
        public static readonly GameNotification StackGrabbed    = new GameNotification { Name = "StackGrabbed",      LogExecution = true  };
        public static readonly GameNotification StackDropped    = new GameNotification { Name = "StackDropped",      LogExecution = true  };
        public static readonly GameNotification MoveMade        = new GameNotification { Name = "MoveMade",          LogExecution = true  };
        public static readonly GameNotification AbortInitiated  = new GameNotification { Name = "AbortInitiated",    LogExecution = true  };
        public static readonly GameNotification AbortCompleted  = new GameNotification { Name = "AbortCompleted",    LogExecution = true  };
        public static readonly GameNotification MoveInitiated   = new GameNotification { Name = "MoveInitiated",     LogExecution = true  };
        public static readonly GameNotification MoveCompleted   = new GameNotification { Name = "MoveCompleted",     LogExecution = true  };
        public static readonly GameNotification UndoInitiated   = new GameNotification { Name = "UndoInitiated",     LogExecution = true  };
        public static readonly GameNotification UndoCompleted   = new GameNotification { Name = "UndoCompleted",     LogExecution = true  };
        public static readonly GameNotification RedoInitiated   = new GameNotification { Name = "RedoInitiated",     LogExecution = true  };
        public static readonly GameNotification RedoCompleted   = new GameNotification { Name = "RedoCompleted",     LogExecution = true  };
        public static readonly GameNotification MoveAborted     = new GameNotification { Name = "MoveAborted",       LogExecution = true  };
        public static readonly GameNotification MoveTracked     = new GameNotification { Name = "MoveTracked",       LogExecution = false };
        public static readonly GameNotification CurrentTurnSet  = new GameNotification { Name = "CurrentTurnSet",    LogExecution = true  };
    }
}
