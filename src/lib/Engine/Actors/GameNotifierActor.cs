using System;
using Akka.Actor;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Actors
{
    public class GameNotifierActor : ReceiveActor
    {
        public IGameActivityTracker Tracker { get; private set; }


        public GameNotifierActor(IGameActivityTracker tracker)
        {
            Receive<GameCreatedMessage>    (m => tracker?.OnGameCreated    (m.Prototype));
            Receive<GameStartedMessage>    (m => tracker?.OnGameStarted    (m.GameId));
            Receive<GameCompletedMessage>  (m => tracker?.OnGameCompleted  (m.GameId));
            Receive<TurnStartedMessage>    (m => tracker?.OnTurnStarted    (m.GameId, m.Turn, m.PlayerId));
            Receive<TurnCompletedMessage>  (m => tracker?.OnTurnCompleted  (m.GameId, m.Turn, m.PlayerId));
            Receive<StoneDrawnMessage>     (m => tracker?.OnStoneDrawn     (m.GameId, m.Move, m.PlayerId));
            Receive<StoneReturnedMessage>  (m => tracker?.OnStoneReturned  (m.GameId, m.Move, m.PlayerId));
            Receive<StonePlacedMessage>    (m => tracker?.OnStonePlaced    (m.GameId, m.Move, m.PlayerId));
            Receive<StackGrabbedMessage>   (m => tracker?.OnStackGrabbed   (m.GameId, m.Move, m.PlayerId));
            Receive<StackDroppedMessage>   (m => tracker?.OnStackDropped   (m.GameId, m.Move, m.PlayerId));
            Receive<MoveAbortedMessage>    (m => tracker?.OnMoveAborted    (m.GameId, m.Move, m.PlayerId));
            Receive<MoveMadeMessage>       (m => tracker?.OnMoveMade       (m.GameId, m.Move, m.PlayerId));
            Receive<AbortInitiatedMessage> (m => tracker?.OnAbortInitiated (m.GameId, m.Move, m.PlayerId, m.Duration));
            Receive<AbortCompletedMessage> (m => tracker?.OnAbortCompleted (m.GameId, m.Move, m.PlayerId));
            Receive<MoveInitiatedMessage>  (m => tracker?.OnMoveInitiated  (m.GameId, m.Move, m.PlayerId, m.Duration));
            Receive<MoveCompletedMessage>  (m => tracker?.OnMoveCompleted  (m.GameId, m.Move, m.PlayerId));
            Receive<UndoInitiatedMessage>  (m => tracker?.OnUndoInitiated  (m.GameId, m.Move, m.PlayerId, m.Duration));
            Receive<UndoCompletedMessage>  (m => tracker?.OnUndoCompleted  (m.GameId, m.Move, m.PlayerId));
            Receive<RedoInitiatedMessage>  (m => tracker?.OnRedoInitiated  (m.GameId, m.Move, m.PlayerId, m.Duration));
            Receive<RedoCompletedMessage>  (m => tracker?.OnRedoCompleted  (m.GameId, m.Move, m.PlayerId));
            Receive<MoveTrackedMessage>    (m => tracker?.OnMoveTracked    (m.GameId, m.Position));
            Receive<CurrentTurnSetMessage> (m => tracker?.OnCurrentTurnSet (m.GameId, m.Turn, m.Stones));
        }


        public static Props Props(IGameActivityTracker tracker)
        {
            return Akka.Actor.Props.Create(() => new GameNotifierActor(tracker));
        }
    }
}
