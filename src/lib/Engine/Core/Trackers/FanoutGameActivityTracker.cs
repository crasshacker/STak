using System;
using System.Linq;
using STak.TakEngine;

namespace STak.TakEngine.Trackers
{
    public class FanoutGameActivityTracker : IGameActivityTracker
    {
        private readonly IGameActivityTracker[] m_trackers;


        private FanoutGameActivityTracker()
        {
        }


        public FanoutGameActivityTracker(params IGameActivityTracker[] trackers)
        {
            m_trackers = new IGameActivityTracker[trackers.Length];
            trackers.CopyTo(m_trackers, 0);
        }


        public void OnGameCreated(GamePrototype prototype)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnGameCreated(prototype);
            }
        }


        public void OnGameStarted(Guid gameId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnGameStarted(gameId);
            }
        }

        public void OnGameCompleted(Guid gameId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnGameCompleted(gameId);
            }
        }

        public void OnTurnStarted(Guid gameId, int turn, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnTurnStarted(gameId, turn, playerId);
            }
        }

        public void OnTurnCompleted(Guid gameId, int turn, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnTurnCompleted(gameId, turn, playerId);
            }
        }


        public void OnStoneDrawn(Guid gameId, StoneMove stoneMove, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnStoneDrawn(gameId, stoneMove, playerId);
            }
        }


        public void OnStoneReturned(Guid gameId, StoneMove stoneMove, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnStoneReturned(gameId, stoneMove, playerId);
            }
        }


        public void OnStonePlaced(Guid gameId, StoneMove stoneMove, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnStonePlaced(gameId, stoneMove, playerId);
            }
        }


        public void OnStackGrabbed(Guid gameId, StackMove stackMove, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnStackGrabbed(gameId, stackMove, playerId);
            }
        }


        public void OnStackDropped(Guid gameId, StackMove stackMove, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnStackDropped(gameId, stackMove, playerId);
            }
        }


        public void OnMoveAborted(Guid gameId, IMove move, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnMoveAborted(gameId, move, playerId);
            }
        }


        public void OnMoveMade(Guid gameId, IMove move, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnMoveMade(gameId, move, playerId);
            }
        }


        public void OnAbortInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnAbortInitiated(gameId, move, playerId, duration);
            }
        }


        public void OnAbortCompleted(Guid gameId, IMove move, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnAbortCompleted(gameId, move, playerId);
            }
        }


        public void OnMoveInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnMoveInitiated(gameId, move, playerId, duration);
            }
        }


        public void OnMoveCompleted(Guid gameId, IMove move, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnMoveCompleted(gameId, move, playerId);
            }
        }


        public void OnUndoInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnUndoInitiated(gameId, move, playerId, duration);
            }
        }


        public void OnUndoCompleted(Guid gameId, IMove move, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnUndoCompleted(gameId, move, playerId);
            }
        }


        public void OnRedoInitiated(Guid gameId, IMove move, int playerId, int duration)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnRedoInitiated(gameId, move, playerId, duration);
            }
        }


        public void OnRedoCompleted(Guid gameId, IMove move, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnRedoCompleted(gameId, move, playerId);
            }
        }


        public void OnMoveCommencing(Guid gameId, IMove move, int playerId)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnMoveCommencing(gameId, move, playerId);
            }
        }


        public void OnCurrentTurnSet(Guid gameId, int turn, Stone[] stones)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnCurrentTurnSet(gameId, turn, stones);
            }
        }


        public void OnMoveTracked(Guid gameId, BoardPosition position)
        {
            foreach (IGameActivityTracker tracker in m_trackers)
            {
                tracker.OnMoveTracked(gameId, position);
            }
        }


        public T GetTracker<T>()
        {
            return (T) m_trackers.Where(t => t.GetType() == typeof(T)).SingleOrDefault();
        }
    }
}
