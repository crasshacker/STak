using System;
using STak.TakEngine;

namespace STak.TakEngine.Trackers
{
    public class DispatchingGameActivityTracker : IGameActivityTracker
    {
        private readonly IDispatcher          m_dispatcher;
        private readonly IGameActivityTracker m_tracker;

        public IGameActivityTracker Tracker { get => m_tracker; }


        public DispatchingGameActivityTracker(IDispatcher dispatcher, IGameActivityTracker tracker)
        {
            m_dispatcher = dispatcher;
            m_tracker    = tracker;
        }

        public void OnGameCreated(GamePrototype prototype)
        {
            m_dispatcher.Invoke(() => m_tracker.OnGameCreated(prototype));
        }

        public void OnGameStarted(Guid gameId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnGameStarted(gameId));
        }

        public void OnGameCompleted(Guid gameId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnGameCompleted(gameId));
        }

        public void OnTurnStarted(Guid gameId, int turn, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnTurnStarted(gameId, turn, playerId));
        }

        public void OnTurnCompleted(Guid gameId, int turn, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnTurnCompleted(gameId, turn, playerId));
        }

        public void OnStoneDrawn(Guid gameId, StoneMove stoneMove, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnStoneDrawn(gameId, stoneMove.Clone() as StoneMove, playerId));
        }

        public void OnStoneReturned(Guid gameId, StoneMove stoneMove, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnStoneReturned(gameId, stoneMove.Clone() as StoneMove, playerId));
        }

        public void OnStonePlaced(Guid gameId, StoneMove stoneMove, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnStonePlaced(gameId, stoneMove.Clone() as StoneMove, playerId));
        }

        public void OnStackGrabbed(Guid gameId, StackMove stackMove, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnStackGrabbed(gameId, stackMove.Clone() as StackMove, playerId));
        }

        public void OnStackDropped(Guid gameId, StackMove stackMove, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnStackDropped(gameId, stackMove.Clone() as StackMove, playerId));
        }

        public void OnMoveAborted(Guid gameId, IMove move, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnMoveAborted(gameId, move.Clone(), playerId));
        }

        public void OnMoveMade(Guid gameId, IMove move, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnMoveMade(gameId, move.Clone(), playerId));
        }

        public void OnAbortInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            m_dispatcher.Invoke(() => m_tracker.OnAbortInitiated(gameId, move, playerId, duration));
        }

        public void OnAbortCompleted(Guid gameId, IMove move, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnAbortCompleted(gameId, move, playerId));
        }

        public void OnMoveInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            m_dispatcher.Invoke(() => m_tracker.OnMoveInitiated(gameId, move.Clone(), playerId, duration));
        }

        public void OnMoveCompleted(Guid gameId, IMove move, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnMoveCompleted(gameId, move.Clone(), playerId));
        }

        public void OnUndoInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            m_dispatcher.Invoke(() => m_tracker.OnUndoInitiated(gameId, move.Clone(), playerId, duration));
        }

        public void OnUndoCompleted(Guid gameId, IMove move, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnUndoCompleted(gameId, move.Clone(), playerId));
        }

        public void OnRedoInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            m_dispatcher.Invoke(() => m_tracker.OnRedoInitiated(gameId, move.Clone(), playerId, duration));
        }

        public void OnRedoCompleted(Guid gameId, IMove move, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnRedoCompleted(gameId, move.Clone(), playerId));
        }

        public void OnMoveCommencing(Guid gameId, IMove move, int playerId)
        {
            m_dispatcher.Invoke(() => m_tracker.OnMoveCommencing(gameId, move.Clone(), playerId));
        }

        public void OnCurrentTurnSet(Guid gameId, int turn, Stone[] stones)
        {
            m_dispatcher.Invoke(() => m_tracker.OnCurrentTurnSet(gameId, turn, stones));
        }

        public void OnMoveTracked(Guid gameId, BoardPosition position)
        {
            m_dispatcher.Invoke(() => m_tracker.OnMoveTracked(gameId, position));
        }
    }
}
