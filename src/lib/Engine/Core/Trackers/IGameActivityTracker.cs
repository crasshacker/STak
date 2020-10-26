using System;
using STak.TakEngine;

namespace STak.TakEngine.Trackers
{
    public interface IGameActivityTracker
    {
        void OnGameCreated    (GamePrototype prototype);
        void OnGameStarted    (Guid gameId);
        void OnGameCompleted  (Guid gameId);
        void OnTurnStarted    (Guid gameId, int turn, int playerId);
        void OnTurnCompleted  (Guid gameId, int turn, int playerId);
        void OnStoneDrawn     (Guid gameId, StoneMove stoneMove, int playerId);
        void OnStoneReturned  (Guid gameId, StoneMove stoneMove, int playerId);
        void OnStonePlaced    (Guid gameId, StoneMove stoneMove, int playerId);
        void OnStackGrabbed   (Guid gameId, StackMove stackMove, int playerId);
        void OnStackDropped   (Guid gameId, StackMove stackMove, int playerId);
        void OnMoveAborted    (Guid gameId, IMove move, int playerId);
        void OnMoveMade       (Guid gameId, IMove move, int playerId);
        void OnAbortInitiated (Guid gameId, IMove move, int playerId, int duration);
        void OnAbortCompleted (Guid gameId, IMove move, int playerId);
        void OnMoveInitiated  (Guid gameId, IMove move, int playerId, int duration);
        void OnMoveCompleted  (Guid gameId, IMove move, int playerId);
        void OnUndoInitiated  (Guid gameId, IMove move, int playerId, int duration);
        void OnUndoCompleted  (Guid gameId, IMove move, int playerId);
        void OnRedoInitiated  (Guid gameId, IMove move, int playerId, int duration);
        void OnRedoCompleted  (Guid gameId, IMove move, int playerId);
        void OnMoveCommencing (Guid gameId, IMove move, int playerId);
        void OnCurrentTurnSet (Guid gameId, int turn, Stone[] stones);
        void OnMoveTracked    (Guid gameId, BoardPosition position);
    }
}
