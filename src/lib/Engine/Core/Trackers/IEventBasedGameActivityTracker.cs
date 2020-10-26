using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using NLog;
using STak.TakEngine;

namespace STak.TakEngine.Trackers
{
    public interface IEventBasedGameActivityTracker : IGameActivityTracker
    {
        event EventHandler<GameCreatedEventArgs>    GameCreated;
        event EventHandler<GameStartedEventArgs>    GameStarted;
        event EventHandler<GameCompletedEventArgs>  GameCompleted;
        event EventHandler<TurnStartedEventArgs>    TurnStarted;
        event EventHandler<TurnCompletedEventArgs>  TurnCompleted;
        event EventHandler<MoveAbortedEventArgs>    MoveAborted;
        event EventHandler<MoveMadeEventArgs>       MoveMade;
        event EventHandler<AbortInitiatedEventArgs> AbortInitiated;
        event EventHandler<AbortCompletedEventArgs> AbortCompleted;
        event EventHandler<MoveInitiatedEventArgs>  MoveInitiated;
        event EventHandler<MoveCompletedEventArgs>  MoveCompleted;
        event EventHandler<UndoInitiatedEventArgs>  UndoInitiated;
        event EventHandler<UndoCompletedEventArgs>  UndoCompleted;
        event EventHandler<RedoInitiatedEventArgs>  RedoInitiated;
        event EventHandler<RedoCompletedEventArgs>  RedoCompleted;
        event EventHandler<MoveCommencingEventArgs> MoveCommencing;
        event EventHandler<StoneDrawnEventArgs>     StoneDrawn;
        event EventHandler<StonePlacedEventArgs>    StonePlaced;
        event EventHandler<StoneReturnedEventArgs>  StoneReturned;
        event EventHandler<StackGrabbedEventArgs>   StackGrabbed;
        event EventHandler<StackDroppedEventArgs>   StackDropped;
        event EventHandler<StackModifiedEventArgs>  StackModified;
        event EventHandler<CurrentTurnSetEventArgs> CurrentTurnSet;
        event EventHandler<MoveTrackedEventArgs>    MoveTracked;
    }
}
