using System;
using System.Threading.Tasks;
using STak.TakEngine;
using STak.TakHub.Interop.Dto;

namespace STak.TakHub.Interop
{
    public interface IGameHubClient
    {
        Task OnGameCreated     (GamePrototypeDto prototype);
        Task OnGameStarted     (Guid gameId);
        Task OnGameCompleted   (Guid gameId);
        Task OnTurnStarted     (Guid gameId, int turn, int playerId);
        Task OnTurnCompleted   (Guid gameId, int turn, int playerId);
        Task OnStoneDrawn      (Guid gameId, StoneMoveDto stoneMove, int playerId);
        Task OnStoneReturned   (Guid gameId, StoneMoveDto stoneMove, int playerId);
        Task OnStonePlaced     (Guid gameId, StoneMoveDto stoneMove, int playerId);
        Task OnStackGrabbed    (Guid gameId, StackMoveDto stackMove, int playerId);
        Task OnStackDropped    (Guid gameId, StackMoveDto stackMove, int playerId);
        Task OnMoveAborted     (Guid gameId, MoveDto move, int playerId);
        Task OnMoveMade        (Guid gameId, MoveDto move, int playerId);
        Task OnAbortInitiated  (Guid gameId, MoveDto move, int playerId, int duration);
        Task OnAbortCompleted  (Guid gameId, MoveDto move, int playerId);
        Task OnMoveInitiated   (Guid gameId, MoveDto move, int playerId, int duration);
        Task OnMoveCompleted   (Guid gameId, MoveDto move, int playerId);
        Task OnUndoInitiated   (Guid gameId, MoveDto move, int playerId, int duration);
        Task OnUndoCompleted   (Guid gameId, MoveDto move, int playerId);
        Task OnRedoInitiated   (Guid gameId, MoveDto move, int playerId, int duration);
        Task OnRedoCompleted   (Guid gameId, MoveDto move, int playerId);
        Task OnMoveCommencing  (Guid gameId, MoveDto move, int playerId);
        Task OnCurrentTurnSet  (Guid gameId, int turn, StoneDto[] moves);
        Task OnMoveTracked     (Guid gameId, BoardPositionDto position);

        Task OnInviteAdded     (GameInviteDto invite);
        Task OnInviteRemoved   (GameInviteDto invite);
        Task OnGameAdded       (GamePrototypeDto prototype, HubGameTypeDto gameType);
        Task OnGameRemoved     (Guid gameId);
        Task OnKibitzerAdded   (GamePrototypeDto prototype, string hubUserName);
        Task OnKibitzerRemoved (GamePrototypeDto prototype, string hubUserName);
        Task OnGameAbandoned   (Guid gameId, string abandonerName);
        Task OnChatMessage     (Guid gameId, string sender, string target, string message);
    }
}
