using System;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Interfaces.Services;
using STak.TakHub.Core.Interfaces.Hubs;
using STak.TakHub.Core.Hubs;
using STak.TakHub.Interop;
using STak.TakEngine;
using NLog;

namespace STak.TakHub.Hubs
{
    public abstract class GameActionUseCase
    {
        public abstract void Handle(GameActionRequest request);


        public GameActionUseCase(IHubGameService gameService)
        {
            m_gameService = gameService;
        }


        protected virtual void PrepareToExecute(GameActionRequest request, bool validateGame = true)
        {
            (m_game, m_playerId) = GetGameTableAndPlayerId(request.Attendee.ConnectionId);

            if (validateGame && m_game == null)
            {
                throw new Exception($"There is no active game for player with ID={request.Attendee.ConnectionId}.");
            }
        }


        protected void ExecuteRequest(Action request)
        {
            try
            {
                request();
            }
            catch (Exception ex)
            {
                s_logger.Debug(ex, $"ExecuteRequest: Caught Excepton: {ex}");
                throw; // TODO - Do something appropriate.
            }
        }


        protected (IGame, int) GetGameTableAndPlayerId(string connectionId)
        {
            Attendee attendee = m_gameService.GetAttendeeForConnection(connectionId);
            GameTable table = m_gameService.GetTableForAttendee(attendee);
            int playerId = GetPlayerIdForConnection(table, connectionId);
            return (table?.Game, playerId);
        }


        private int GetPlayerIdForConnection(GameTable table, string connectionId)
        {
            return (table != null) ? ((table.Player1Id == connectionId) ? Player.One
                                   :  (table.Player2Id == connectionId) ? Player.Two
                                                         : Player.None) : Player.None;
        }


        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        protected IGame           m_game;
        protected int             m_playerId;
        protected IHubGameService m_gameService;
    }


    public class SetPropertyUseCase : GameActionUseCase
    {
        public SetPropertyUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            ExecuteRequest(() =>
            {
                var typedRequest = request as SetPropertyRequest;
                var attendee = typedRequest.Attendee;
                string name  = typedRequest.Name;
                string value = typedRequest.Value;
                m_gameService.SetProperty(attendee, name, value);
            });
        }
    }


    public class RequestActiveInvitesUseCase : GameActionUseCase
    {
        public RequestActiveInvitesUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            ExecuteRequest(async () =>
            {
                await m_gameService.RequestActiveInvites(request.Attendee);
            });
        }
    }


    public class RequestActiveGamesUseCase : GameActionUseCase
    {
        public RequestActiveGamesUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            ExecuteRequest(async () =>
            {
                await m_gameService.RequestActiveGames(request.Attendee);
            });
        }
    }


    public class InviteGameUseCase : GameActionUseCase
    {
        public InviteGameUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            ExecuteRequest(async () =>
            {
                var typedRequest = request as InviteGameRequest;
                GameInvite invite = typedRequest.Invite;
                await m_gameService.InviteGame(invite);
            });
        }
    }


    public class KibitzGameUseCase : GameActionUseCase
    {
        public KibitzGameUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            ExecuteRequest(async () =>
            {
                var typedRequest = request as KibitzGameRequest;
                Attendee attendee = typedRequest.Attendee;
                Guid gameId = typedRequest.GameId;
                await m_gameService.KibitzGame(attendee, gameId);
            });
        }
    }


    public class AcceptGameUseCase : GameActionUseCase
    {
        public AcceptGameUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            ExecuteRequest(() => m_gameService.AcceptGame(request.Attendee.ConnectionId));
        }
    }


    public class InitializeGameUseCase : GameActionUseCase
    {
        public InitializeGameUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            ExecuteRequest(() => m_game.Initialize());
        }
    }


    public class StartGameUseCase : GameActionUseCase
    {
        public StartGameUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            ExecuteRequest(() => m_game.Start());
        }
    }


    public class QuitGameUseCase : GameActionUseCase
    {
        public QuitGameUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as QuitGameRequest;
            ExecuteRequest(() => m_gameService.QuitGame(request.Attendee, typedRequest.GameId));
        }
    }


    public class ChatUseCase : GameActionUseCase
    {
        public ChatUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request, false);
            var typedRequest = request as ChatRequest;
            ExecuteRequest(() => m_gameService.Chat(request.Attendee, typedRequest.GameId, typedRequest.Target, typedRequest.Message));
        }
    }


    public class DrawStoneUseCase : GameActionUseCase
    {
        public DrawStoneUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as DrawStoneRequest;
            ExecuteRequest(() => m_game.DrawStone(m_playerId, typedRequest.StoneType, typedRequest.StoneId));
        }
    }


    public class ReturnStoneUseCase : GameActionUseCase
    {
        public ReturnStoneUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            ExecuteRequest(() => m_game.ReturnStone(m_playerId));
        }
    }


    public class PlaceStoneUseCase : GameActionUseCase
    {
        public PlaceStoneUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as PlaceStoneRequest;
            ExecuteRequest(() => m_game.PlaceStone(m_playerId, typedRequest.Cell, typedRequest.StoneType));
        }
    }


    public class GrabStackUseCase : GameActionUseCase
    {
        public GrabStackUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as GrabStackRequest;
            ExecuteRequest(() => m_game.GrabStack(m_playerId, typedRequest.Cell, typedRequest.StoneCount));
        }
    }


    public class DropStackUseCase : GameActionUseCase
    {
        public DropStackUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as DropStackRequest;
            ExecuteRequest(() => m_game.DropStack(m_playerId, typedRequest.Cell, typedRequest.StoneCount));
        }
    }


    public class MakeMoveUseCase : GameActionUseCase
    {
        public MakeMoveUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as MakeMoveRequest;
            ExecuteRequest(() => m_game.MakeMove(m_playerId, typedRequest.Move));
        }
    }


    public class UndoMoveUseCase : GameActionUseCase
    {
        public UndoMoveUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            ExecuteRequest(() => m_game.UndoMove(Player.None));
        }
    }


    public class RedoMoveUseCase : GameActionUseCase
    {
        public RedoMoveUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            ExecuteRequest(() => m_game.RedoMove(Player.None));
        }
    }


    public class AbortMoveUseCase : GameActionUseCase
    {
        public AbortMoveUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            ExecuteRequest(() => m_game.AbortMove(m_playerId));
        }
    }


    public class InitiateAbortUseCase : GameActionUseCase
    {
        public InitiateAbortUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as InitiateAbortRequest;
            ExecuteRequest(() => m_game.InitiateAbort(m_playerId, typedRequest.Move, typedRequest.Duration));
        }
    }


    public class CompleteAbortUseCase : GameActionUseCase
    {
        public CompleteAbortUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as CompleteAbortRequest;
            ExecuteRequest(() => m_game.CompleteAbort(m_playerId, typedRequest.Move));
        }
    }


    public class InitiateMoveUseCase : GameActionUseCase
    {
        public InitiateMoveUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as InitiateMoveRequest;
            ExecuteRequest(() => m_game.InitiateMove(Player.None, typedRequest.Move, typedRequest.Duration));
        }
    }


    public class CompleteMoveUseCase : GameActionUseCase
    {
        public CompleteMoveUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as CompleteMoveRequest;
            ExecuteRequest(() => m_game.CompleteMove(Player.None, typedRequest.Move));
        }
    }


    public class InitiateUndoUseCase : GameActionUseCase
    {
        public InitiateUndoUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as InitiateUndoRequest;
            ExecuteRequest(() => m_game.InitiateUndo(Player.None, typedRequest.Duration));
        }
    }


    public class CompleteUndoUseCase : GameActionUseCase
    {
        public CompleteUndoUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            ExecuteRequest(() => m_game.CompleteUndo(Player.None));
        }
    }


    public class InitiateRedoUseCase : GameActionUseCase
    {
        public InitiateRedoUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as InitiateRedoRequest;
            ExecuteRequest(() => m_game.InitiateRedo(Player.None, typedRequest.Duration));
        }
    }


    public class CompleteRedoUseCase : GameActionUseCase
    {
        public CompleteRedoUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            ExecuteRequest(() => m_game.CompleteRedo(Player.None));
        }
    }


    public class TrackMoveUseCase : GameActionUseCase
    {
        public TrackMoveUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as TrackMoveRequest;
            ExecuteRequest(() => m_game.TrackMove(m_playerId, typedRequest.Position));
        }
    }


    public class SetCurrentTurnUseCase : GameActionUseCase
    {
        public SetCurrentTurnUseCase(IHubGameService gameService)
            : base(gameService)
        {
        }

        public override void Handle(GameActionRequest request)
        {
            PrepareToExecute(request);
            var typedRequest = request as SetCurrentTurnRequest;
            ExecuteRequest(() => m_game.SetCurrentTurn(m_playerId, typedRequest.Turn));
        }
    }
}
