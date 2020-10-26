using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using STak.TakHub.Core.Domain.Entities;
using STak.TakHub.Core.Interfaces.Hubs;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.UseCases;
using STak.TakHub.Core.Hubs;
using STak.TakHub.Interop;
using STak.TakHub.Interop.Dto;
using STak.TakEngine;
using NLog;

namespace STak.TakHub.Hubs
{
    [Authorize(Policy="ApiUser")]
    public class GameHub : Hub<IGameHubClient>
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private readonly IHubGameService m_gameService;

        private Attendee Me => m_gameService.GetAttendeeForConnection(Context.ConnectionId);


        public GameHub(IHubGameService gameService)
        {
            m_gameService = gameService;
        }


        public string GetHubLoginName()
        {
            LogRequest("GetHubLoginName");
            return Context.UserIdentifier;
        }


        public void UpdateConnectionId(string oldId, string newId)
        {
            LogRequest("UpdateConnectionId");
            m_gameService.UpdateConnectionId(oldId, newId);
        }


        public string GetHubUserName()
        {
            LogRequest("GetHubUserName");
            return Me.UserName;
        }


        public void SetProperty(string name, string value)
        {
            HandleHubRequest("SetProperty", () =>
            {
                var useCase = new SetPropertyUseCase(m_gameService);
                useCase.Handle(new SetPropertyRequest(Me, name, value));
            });
        }


        public void RequestActiveInvites()
        {
            HandleHubRequest("RequestActiveInvites", () =>
            {
                var useCase = new RequestActiveInvitesUseCase(m_gameService);
                useCase.Handle(new RequestActiveInvitesRequest(Me));
            });
        }


        public void RequestActiveGames()
        {
            HandleHubRequest("RequestActiveGames", () =>
            {
                var useCase = new RequestActiveGamesUseCase(m_gameService);
                useCase.Handle(new RequestActiveGamesRequest(Me));
            });
        }


        public void InviteGame(GameInviteDto inviteDto)
        {
            HandleHubRequest("InviteGame", () =>
            {
                GameInvite invite = Mapper.Map<GameInvite>(inviteDto);
                PrepareInvite(invite);
                var useCase = new InviteGameUseCase(m_gameService);
                useCase.Handle(new InviteGameRequest(Me, invite));
            });
        }


        public void KibitzGame(Guid gameId)
        {
            HandleHubRequest("KibitzGame", () =>
            {
                var useCase = new KibitzGameUseCase(m_gameService);
                useCase.Handle(new KibitzGameRequest(Me, gameId));
            });
        }


        public void AcceptGame()
        {
            HandleHubRequest("AcceptGame", () =>
            {
                var useCase = new AcceptGameUseCase(m_gameService);
                useCase.Handle(new AcceptGameRequest(Me));
            });
        }


        public void Initialize()
        {
            HandleHubRequest("Initialize", () =>
            {
                var useCase = new InitializeGameUseCase(m_gameService);
                useCase.Handle(new InitializeGameRequest(Me));
            });
        }


        public void Start()
        {
            HandleHubRequest("Start", () =>
            {
                var useCase = new StartGameUseCase(m_gameService);
                useCase.Handle(new StartGameRequest(Me));
            });
        }


        public void QuitGame(Guid gameId)
        {
            HandleHubRequest("QuitGame", () =>
            {
                var useCase = new QuitGameUseCase(m_gameService);
                useCase.Handle(new QuitGameRequest(Me, gameId));
            });
        }


        public void Chat(Guid gameId, string target, string message)
        {
            HandleHubRequest("Chat", () =>
            {
                var useCase = new ChatUseCase(m_gameService);
                useCase.Handle(new ChatRequest(Me, gameId, target, message));
            });
        }


        public void DrawStone(Guid gameId, int playerId, StoneType stoneType, int stoneId)
        {
            HandleHubRequest("DrawStone", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new DrawStoneUseCase(m_gameService);
                useCase.Handle(new DrawStoneRequest(Me, stoneType, stoneId));
            });
        }


        public void ReturnStone(Guid gameId, int playerId)
        {
            HandleHubRequest("ReturnStone", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new ReturnStoneUseCase(m_gameService);
                useCase.Handle(new ReturnStoneRequest(Me));
            });
        }


        public void PlaceStone(Guid gameId, int playerId, CellDto cellDto, StoneType stoneType)
        {
            HandleHubRequest("PlaceStone", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                Cell cell = Mapper.Map<Cell>(cellDto);
                var useCase = new PlaceStoneUseCase(m_gameService);
                useCase.Handle(new PlaceStoneRequest(Me, cell, stoneType));
            });
        }


        public void GrabStack(Guid gameId, int playerId, CellDto cellDto, int stoneCount)
        {
            HandleHubRequest("GrabStack", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                Cell cell = Mapper.Map<Cell>(cellDto);
                var useCase = new GrabStackUseCase(m_gameService);
                useCase.Handle(new GrabStackRequest(Me, cell, stoneCount));
            });
        }


        public void DropStack(Guid gameId, int playerId, CellDto cellDto, int stoneCount)
        {
            HandleHubRequest("DropStack", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                Cell cell = Mapper.Map<Cell>(cellDto);
                var useCase = new DropStackUseCase(m_gameService);
                useCase.Handle(new DropStackRequest(Me, cell, stoneCount));
            });
        }


        public void MakeMove(Guid gameId, int playerId, MoveDto moveDto)
        {
            HandleHubRequest("MakeMove", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                IMove move = Mapper.Map<IMove>(moveDto);
                var useCase = new MakeMoveUseCase(m_gameService);
                useCase.Handle(new MakeMoveRequest(Me, move));
            });
        }


        public void UndoMove(Guid gameId, int playerId)
        {
            HandleHubRequest("UndoMove", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new UndoMoveUseCase(m_gameService);
                useCase.Handle(new UndoMoveRequest(Me));
            });
        }


        public void RedoMove(Guid gameId, int playerId)
        {
            HandleHubRequest("RedoMove", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new RedoMoveUseCase(m_gameService);
                useCase.Handle(new RedoMoveRequest(Me));
            });
        }


        public void InitiateAbort(Guid gameId, int playerId, MoveDto moveDto, int duration)
        {
            HandleHubRequest("InitiateAbort", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                IMove move = Mapper.Map<IMove>(moveDto);
                var useCase = new InitiateAbortUseCase(m_gameService);
                useCase.Handle(new InitiateAbortRequest(Me, move, duration));
            });
        }


        public void CompleteAbort(Guid gameId, int playerId, MoveDto moveDto)
        {
            HandleHubRequest("CompleteAbort", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                IMove move = Mapper.Map<IMove>(moveDto);
                var useCase = new CompleteAbortUseCase(m_gameService);
                useCase.Handle(new CompleteAbortRequest(Me, move));
            });
        }


        public void InitiateMove(Guid gameId, int playerId, MoveDto moveDto, int duration)
        {
            HandleHubRequest("InitiateMove", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                IMove move = Mapper.Map<IMove>(moveDto);
                var useCase = new InitiateMoveUseCase(m_gameService);
                useCase.Handle(new InitiateMoveRequest(Me, move, duration));
            });
        }


        public void CompleteMove(Guid gameId, int playerId, MoveDto moveDto)
        {
            HandleHubRequest("CompleteMove", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                IMove move = Mapper.Map<IMove>(moveDto);
                var useCase = new CompleteMoveUseCase(m_gameService);
                useCase.Handle(new CompleteMoveRequest(Me, move));
            });
        }


        public void InitiateUndo(Guid gameId, int playerId, int duration)
        {
            HandleHubRequest("InitiateUndo", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new InitiateUndoUseCase(m_gameService);
                useCase.Handle(new InitiateUndoRequest(Me, duration));
            });
        }


        public void CompleteUndo(Guid gameId, int playerId)
        {
            HandleHubRequest("CompleteUndo", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new CompleteUndoUseCase(m_gameService);
                useCase.Handle(new CompleteUndoRequest(Me));
            });
        }


        public void InitiateRedo(Guid gameId, int playerId, int duration)
        {
            HandleHubRequest("InitiateRedo", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new InitiateRedoUseCase(m_gameService);
                useCase.Handle(new InitiateRedoRequest(Me, duration));
            });
        }


        public void CompleteRedo(Guid gameId, int playerId)
        {
            HandleHubRequest("CompleteRedo", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new CompleteRedoUseCase(m_gameService);
                useCase.Handle(new CompleteRedoRequest(Me));
            });
        }


        public void AbortMove(Guid gameId, int playerId)
        {
            HandleHubRequest("AbortMove", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new AbortMoveUseCase(m_gameService);
                useCase.Handle(new AbortMoveRequest(Me));
            });
        }


        public void TrackMove(Guid gameId, int playerId, BoardPositionDto positionDto)
        {
            HandleHubRequest("TrackMove", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                BoardPosition position = Mapper.Map<BoardPosition>(positionDto);
                var useCase = new TrackMoveUseCase(m_gameService);
                useCase.Handle(new TrackMoveRequest(Me, position));
            });
        }


        public void SetCurrentTurn(Guid gameId, int playerId, int turn)
        {
            HandleHubRequest("TrackMove", () =>
            {
                ValidateGameAndPlayerIds(gameId, playerId);
                var useCase = new SetCurrentTurnUseCase(m_gameService);
                useCase.Handle(new SetCurrentTurnRequest(Me, turn));
            });
        }


        public override async Task OnConnectedAsync()
        {
            LogRequest("OnConnectedAsync");
            m_gameService.HandleAttendeeConnection(Context.UserIdentifier, Context.ConnectionId);
            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception ex)
        {
            LogRequest("OnDisconnectedAsync");
            s_logger.Debug("Disconnecting connection={0} - {1}", Context.ConnectionId, ex);
            await m_gameService.HandleAttendeeDisconnection(Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }


        private void PrepareInvite(GameInvite invite)
        {
            var attendee = Me;
            invite.Inviter = attendee.UserName;
            invite.ConnectionId = attendee.ConnectionId;
        }


        private void ValidateGameAndPlayerIds(Guid gameId, int playerId)
        {
            var table = m_gameService.GetTableForAttendee(Me);

            if (table == null)
            {
                throw new Exception($"User {Me.UserName} is not present at the table for game with ID={gameId}.");
            }

            if (playerId != Player.None)
            {
                if ((table.PlayerOne == Me && playerId != Player.One) ||
                    (table.PlayerTwo == Me && playerId != Player.Two))
                {
                    throw new Exception("The player number does not match the player's position in the game.");
                }
            }
        }


        private void HandleHubRequest(string requestName, Action requestAction)
        {
            try
            {
                LogRequest(requestName);
                requestAction();
            }
            catch (Exception ex)
            {
                s_logger.Debug($"Caught exception while responding to the {requestName} request: {ex.Message}");
                // Ignore requests that might reasonably appear and are innocuous.
                if (requestName != "QuitGame" && requestName != "TrackMove")
                {
                    throw;
                }
            }
        }


        private void LogRequest(string methodName)
        {
            if (methodName != "TrackMove")
            {
                s_logger.Debug("Connection[{0}] - {1}", Context.ConnectionId, methodName);
            }
        }
    }
}
