using System;
using Akka.Actor;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Actors
{
    public class ForwardingGameActivityTracker : IGameActivityTracker
    {
        private IActorRef NotifierActor { get; set; }
        private IActorRef GameActor     { get; set; }


        public ForwardingGameActivityTracker(IActorRef gameActor, IActorRef notifierActor)
        {
            NotifierActor = notifierActor;
            GameActor     = gameActor;
        }

        public void OnGameCreated(GamePrototype prototype)                                => Tell(new GameCreatedMessage    (prototype));
        public void OnGameStarted(Guid gameId)                                            => Tell(new GameStartedMessage    (gameId));
        public void OnGameCompleted(Guid gameId)                                          => Tell(new GameCompletedMessage  (gameId));
        public void OnTurnStarted(Guid gameId, int turn, int playerId)                    => Tell(new TurnStartedMessage    (gameId, turn, playerId));
        public void OnTurnCompleted(Guid gameId, int turn, int playerId)                  => Tell(new TurnCompletedMessage  (gameId, turn, playerId));
        public void OnStoneDrawn(Guid gameId, StoneMove stoneMove, int playerId)          => Tell(new StoneDrawnMessage     (gameId, stoneMove, playerId));
        public void OnStoneReturned(Guid gameId, StoneMove stoneMove, int playerId)       => Tell(new StoneReturnedMessage  (gameId, stoneMove, playerId));
        public void OnStonePlaced(Guid gameId, StoneMove stoneMove, int playerId)         => Tell(new StonePlacedMessage    (gameId, stoneMove, playerId));
        public void OnStackGrabbed(Guid gameId, StackMove stackMove, int playerId)        => Tell(new StackGrabbedMessage   (gameId, stackMove, playerId));
        public void OnStackDropped(Guid gameId, StackMove stackMove, int playerId)        => Tell(new StackDroppedMessage   (gameId, stackMove, playerId));
        public void OnMoveAborted(Guid gameId, IMove move, int playerId)                  => Tell(new MoveAbortedMessage    (gameId, move, playerId));
        public void OnMoveMade(Guid gameId, IMove move, int playerId)                     => Tell(new MoveMadeMessage       (gameId, move, playerId));
        public void OnAbortInitiated(Guid gameId, IMove move, int playerId, int duration) => Tell(new AbortInitiatedMessage (gameId, move, playerId, duration));
        public void OnAbortCompleted(Guid gameId, IMove move, int playerId)               => Tell(new AbortCompletedMessage (gameId, move, playerId));
        public void OnMoveInitiated(Guid gameId, IMove move, int playerId, int duration)  => Tell(new MoveInitiatedMessage  (gameId, move, playerId, duration));
        public void OnMoveCompleted(Guid gameId, IMove move, int playerId)                => Tell(new MoveCompletedMessage  (gameId, move, playerId));
        public void OnUndoInitiated(Guid gameId, IMove move, int playerId, int duration)  => Tell(new UndoInitiatedMessage  (gameId, move, playerId, duration));
        public void OnUndoCompleted(Guid gameId, IMove move, int playerId)                => Tell(new UndoCompletedMessage  (gameId, move, playerId));
        public void OnRedoInitiated(Guid gameId, IMove move, int playerId, int duration)  => Tell(new RedoInitiatedMessage  (gameId, move, playerId, duration));
        public void OnRedoCompleted(Guid gameId, IMove move, int playerId)                => Tell(new RedoCompletedMessage  (gameId, move, playerId));
        public void OnMoveCommencing(Guid gameId, IMove move, int playerId)               => Tell(new MoveCommencingMessage (gameId, move, playerId));
        public void OnCurrentTurnSet(Guid gameId, int turn, Stone[] stones)               => Tell(new CurrentTurnSetMessage (gameId, turn, stones));
        public void OnMoveTracked(Guid gameId, BoardPosition position)                    => Tell(new MoveTrackedMessage    (gameId, position));

        private void Tell(GameActivityActorMessage message) => NotifierActor.Tell(message, GameActor);
    }
}
