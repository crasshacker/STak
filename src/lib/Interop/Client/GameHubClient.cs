#define NEWTONSOFT_JSON_SERIALIZATION
#define SYSTEMTEXT_JSON_SERIALIZATION
#define MESSAGEPACK_SERIALIZATION

using System;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;
#if NEWTONSOFT_JSON_SERIALIZATION
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#endif
using NLog;
using NLog.Extensions.Logging;
using STak.TakEngine;
using STak.TakEngine.AI;
using STak.TakEngine.Management;
using STak.TakHub.Interop;
using STak.TakHub.Interop.Dto;
using STak.TakHub.Client.Trackers;

namespace STak.TakHub.Client
{
    public enum HubClientState
    {
        Connecting,
        Connected,
        Reconnecting,
        Reconnected,
        Disconnecting,
        Disconnected
    }


    public class GameHubClient
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private string                                             m_connectionId;
        private readonly GameManager                               m_gameManager;
        private readonly ConcurrentDictionary<Guid, GamePrototype> m_gamePrototypes;
        private readonly ConcurrentDictionary<Guid, IGame>         m_games;
        private readonly HubGameOptions                            m_options;
        private readonly IEventBasedHubActivityTracker             m_tracker;

        public IGameBuilder  GameBuilder      { get; set; }
        public HubConnection Connection       { get; private set; }
        public Exception     ConnectException { get; private set; }
        public string        UserName         { get; private set; }

        public IEventBasedHubActivityTracker Tracker => m_tracker;

        public bool IsConnected => Connection != null && Connection.State == HubConnectionState.Connected;


        public GameHubClient(GameManager gameManager, IEventBasedHubActivityTracker tracker,
                                                              HubGameOptions options = null)
        {
            m_gameManager    = gameManager;
            m_options        = options ?? new HubGameOptions();
            m_gamePrototypes = new ConcurrentDictionary<Guid, GamePrototype>();
            m_games          = new ConcurrentDictionary<Guid, IGame>();
            m_tracker        = tracker;
        }


        public async Task Connect(Uri gameHubUri, Authenticator authenticator, CancellationToken canceller = default)
        {
            s_logger.Debug("Building connection to TakHub server.");
            Connection = BuildConnection(gameHubUri, authenticator);

            if (Connection.State == HubConnectionState.Disconnected)
            {
                ConnectException = null;

                try
                {
                    OnStateChange(HubClientState.Connecting);
                    await Connection.StartAsync(canceller);
                    m_connectionId = Connection.ConnectionId;
                    RegisterHubCallbacks();
                }
                catch (Exception ex)
                {
                    ConnectException = ex;
                    s_logger.Debug(ex, $"Connect: Caught exception: {ex.Message}");
                }

                if (Connection.State == HubConnectionState.Connected)
                {
                    OnStateChange(HubClientState.Connected);
                    UserName = await GetHubUserName();
                }
                else
                {
                    OnStateChange(HubClientState.Disconnected);
                }
            }
        }


        public IGame GetGame(Guid gameId)
        {
            m_games.TryGetValue(gameId, out IGame game);

            if (game == null)
            {
                // TODO - This shouldn't happen, but what should we do if it does?
                s_logger.Debug($"Hub client cannot find game with Id={gameId}.");
            }

            return game;
        }


        public void RegisterGame(IGame game)
        {
            m_games[game.Id] = game;
        }


        public GamePrototype GetGamePrototype(Guid gameId)
        {
            return GetGame(gameId)?.Prototype;
        }


        public async Task Disconnect()
        {
            if (Connection != null && Connection.State == HubConnectionState.Connected)
            {
                OnStateChange(HubClientState.Disconnecting);
                await Connection.StopAsync();
                UnregisterHubCallbacks();
                m_connectionId = null;
            }
        }


        public async Task<string> GetHubUserName()
        {
            return await HubCallMediator.InvokeCommandAsync<string>(HubCommand.GetHubUserName, async () =>
            {
                return await Connection.InvokeAsync<string>("GetHubUserName");
            });
        }


        public async Task SetProperty(string name, string value)
        {
            await HubCallMediator.InvokeCommandAsync(HubCommand.SetProperty, async () =>
            {
                await Connection.InvokeAsync<string>("SetProperty", name, value);
            });
        }


        public async Task AcceptGame()
        {
            await HubCallMediator.InvokeCommandAsync(HubCommand.RequestActiveInvites, async () =>
            {
                await Connection.InvokeAsync("AcceptGame");
            });
        }


        public async Task RequestActiveInvites()
        {
            await HubCallMediator.InvokeCommandAsync(HubCommand.RequestActiveInvites, async () =>
            {
                await Connection.InvokeAsync("RequestActiveInvites");
            });
        }


        public async Task RequestActiveGames()
        {
            await HubCallMediator.InvokeCommandAsync(HubCommand.RequestActiveGames, async () =>
            {
                await Connection.InvokeAsync("RequestActiveGames");
            });
        }


        public async Task InviteGame(GameInvite invite)
        {
            await HubCallMediator.InvokeCommandAsync(HubCommand.InviteGame, async () =>
            {
                s_logger.Debug("*** Invite:\n" + JsonConvert.SerializeObject(invite));
                var inviteDto = Mapper.Map<GameInviteDto>(invite);
                await Connection.InvokeAsync("InviteGame", inviteDto);
            });
        }


        public async Task KibitzGame(Guid gameId)
        {
            await HubCallMediator.InvokeCommandAsync(HubCommand.KibitzGame, async () =>
            {
                s_logger.Debug("*** GameId: " + JsonConvert.SerializeObject(gameId));
                await Connection.InvokeAsync("KibitzGame", gameId);
            });
        }


        public async Task QuitGame(Guid gameId)
        {
            await HubCallMediator.InvokeCommandAsync(HubCommand.QuitGame, async () =>
            {
                try
                {
                    s_logger.Debug("*** GameId: " + JsonConvert.SerializeObject(gameId));
                    await Connection.InvokeAsync("QuitGame", gameId);
                }
                catch (Exception ex)
                {
                    s_logger.Error($"[Ignorable] Remote invocation of QuitGame command failed: {ex.Message}");
                }
            });
        }


        public async Task Chat(Guid gameId, string target, string message)
        {
            await HubCallMediator.InvokeCommandAsync(HubCommand.Chat, async () =>
            {
                s_logger.Debug("*** GameId: " + JsonConvert.SerializeObject(gameId));
                await Connection.InvokeAsync("Chat", gameId, target, message);
            });
        }


        public virtual void OnStateChange(HubClientState state)
        {
            // Derived classes should override to act on state changes.
            s_logger.Debug($"Hub connection changed state to {Enum.GetName(typeof(HubClientState), state)}.");
        }


        // ***** Game Room Callback Methods


        protected virtual void OnGameCreated(GamePrototype prototype)
        {
            HubCallMediator.ProcessNotification(HubNotification.GameCreated, () =>
            {
                UpdatePrototypePlayerLocations(prototype);
                m_gamePrototypes[prototype.Id] = prototype;

#if ENABLE_LOCAL_AI_VS_REMOTE_AI
                //
                // NOTE: This code is here to support testing of a local AI playing a remote AI.  Whether such a
                //       case should be supported in the real world is TBD, but it serves as a good test to verify
                //       everything works as expected.  Note that in order to reinstate this code, the setters for
                //       the PlayerOne and PlayerTwo properties of the GamePrototype class must be made public.
                //
                if (m_options.UseLocalAI)
                {
                    if (prototype.PlayerOne.IsHuman)
                    {
                        ITakAI takAI = TakAI.GetAI(TakAI.GetAINames().First());
                        prototype.PlayerOne = new Player(takAI.Name, takAI);
                    }
                    if (prototype.PlayerTwo.IsHuman)
                    {
                        ITakAI takAI = TakAI.GetAI(TakAI.GetAINames().First());
                        prototype.PlayerTwo = new Player(takAI.Name, takAI);
                    }
                }
#endif

                IGame game = m_gameManager.CreateGame(GameBuilder, prototype);
                s_logger.Debug($"Created client game for remote game: {game.Id}");
                m_games[game.Id] = game;
                game.Tracker.OnGameCreated(prototype);
            });
        }


        protected virtual void OnGameStarted(Guid gameId)
        {
            HubCallMediator.ProcessNotification(GameNotification.GameStarted, () =>
            {
                IGame game = m_games[gameId];
                game.Tracker.OnGameStarted(gameId);
            });
        }


        protected virtual void OnGameCompleted(Guid gameId)
        {
            HubCallMediator.ProcessNotification(GameNotification.GameCompleted, () =>
            {
                IGame game = m_games[gameId];
                game.Tracker.OnGameCompleted(gameId);
            });
        }


        protected virtual void OnKibitzerAdded(GamePrototype prototype, string hubUserName)
        {
            HubCallMediator.ProcessNotification(HubNotification.KibitzerAdded, () =>
            {
                s_logger.Debug($"*** Hub Username: {hubUserName}");
                UpdatePrototypePlayerLocations(prototype);
                m_tracker.OnKibitzerAdded(prototype, hubUserName);
            });
        }


        protected virtual void OnKibitzerRemoved(GamePrototype prototype, string hubUserName)
        {
            HubCallMediator.ProcessNotification(HubNotification.KibitzerRemoved, () =>
            {
                s_logger.Debug($"*** Hub Username: {hubUserName}");
                UpdatePrototypePlayerLocations(prototype);
                m_tracker.OnKibitzerRemoved(prototype, hubUserName);
            });
        }


        protected virtual void OnInviteAdded(GameInvite invite)
        {
            HubCallMediator.ProcessNotification(HubNotification.InviteAdded, () =>
            {
                s_logger.Debug("*** Invite:\n" + JsonConvert.SerializeObject(invite));
                m_tracker.OnInviteAdded(invite);
            });
        }


        protected virtual void OnInviteRemoved(GameInvite invite)
        {
            HubCallMediator.ProcessNotification(HubNotification.InviteRemoved, () =>
            {
                s_logger.Debug("*** Invite:\n" + JsonConvert.SerializeObject(invite));
                m_tracker.OnInviteRemoved(invite);
            });
        }


        protected virtual void OnGameAdded(GamePrototype prototype, HubGameType gameType)
        {
            HubCallMediator.ProcessNotification(HubNotification.GameAdded, () =>
            {
                s_logger.Debug("*** Prototype:\n" + JsonConvert.SerializeObject(prototype));
                UpdatePrototypePlayerLocations(prototype);
                m_tracker.OnGameAdded(prototype, gameType);
            });
        }


        protected virtual void OnGameRemoved(Guid gameId)
        {
            HubCallMediator.ProcessNotification(HubNotification.GameRemoved, () =>
            {
                s_logger.Debug("*** GameId: " + JsonConvert.SerializeObject(gameId));
                m_tracker.OnGameRemoved(gameId);
            });
        }


        protected virtual void OnGameAbandoned(Guid gameId, string abandonerName)
        {
            HubCallMediator.ProcessNotification(HubNotification.GameAbandoned, () =>
            {
                s_logger.Debug("*** GameId: " + JsonConvert.SerializeObject(gameId));
                s_logger.Debug($"*** Abandoner: {abandonerName}");
                m_tracker.OnGameAbandoned(gameId, abandonerName);
            });
        }


        protected virtual void OnChatMessage(Guid gameId, string sender, string target, string message)
        {
            HubCallMediator.ProcessNotification(HubNotification.ChatMessage, () =>
            {
                s_logger.Debug("*** GameId: " + JsonConvert.SerializeObject(gameId));
                s_logger.Debug($"*** Message: {message}");
                m_tracker.OnChatMessage(gameId, sender, target, message);
            });
        }


        private async Task UpdateConnectionId(string connectionId)
        {
            s_logger.Debug($"Updating connection Id={m_connectionId} to new Id={connectionId}.");
            await Connection.InvokeAsync("UpdateConnectionId", m_connectionId, connectionId);
            m_connectionId = connectionId;
        }


        private void UpdatePrototypePlayerLocations(GamePrototype prototype)
        {
            if (prototype.PlayerOne.Name == UserName)
            {
                prototype.PlayerOne.IsRemote = false;
                prototype.PlayerTwo.IsRemote = true;
            }
            else if (prototype.PlayerTwo.Name == UserName)
            {
                prototype.PlayerOne.IsRemote = true;
                prototype.PlayerTwo.IsRemote = false;
            }
            else
            {
                prototype.PlayerOne.IsRemote = true;
                prototype.PlayerTwo.IsRemote = true;
            }
        }


        private HubConnection BuildConnection(Uri hubUri, Authenticator authenticator)
        {
            HubConnection connection = null;

            var protocol   = InteropAppConfig.SignalR.Protocol;
            var reconnect  = InteropAppConfig.SignalR.AutoReconnect;
            var timeout    = InteropAppConfig.SignalR.ServerTimeout;

            try
            {
                var builder = new HubConnectionBuilder()
                    .ConfigureLogging(logging =>
                    {
                        logging.AddProvider(new NLogLoggerProvider());
                        // NOTE: This will set ALL logging to Debug level.
                        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                    })
                    .WithUrl(hubUri, options =>
                    {
                        options.AccessTokenProvider = async () =>
                        {
                            var tokenInfo = await authenticator.Authenticate();
                            return tokenInfo.Token;
                        };
                    });

                if (reconnect)
                {
                    builder = builder.WithAutomaticReconnect();
                }

                switch (protocol)
                {
#if SYSTEMTEXT_JSON_SERIALIZATION
                    case SignalRProtocol.SystemTextJson:
                    {
                        builder = builder.AddJsonProtocol(options =>
                        {
                        });
                        break;
                    }
#endif

#if NEWTONSOFT_JSON_SERIALIZATION
                    case SignalRProtocol.NewtonsoftJson:
                    {
                        builder = builder.AddNewtonsoftJsonProtocol(options =>
                        {
                            options.PayloadSerializerSettings.TypeNameHandling = TypeNameHandling.All;
                            options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver
                                                                     { IgnoreSerializableAttribute = false };
                        });
                        break;
                    }
#endif

#if MESSAGEPACK_SERIALIZATION
                    case SignalRProtocol.MessagePack:
                    {
                        builder = builder.AddMessagePackProtocol(options =>
                        {
                            options.SerializerOptions = MessagePack.MessagePackSerializerOptions.Standard;
                        });
                        break;
                    }
#endif
                }

                connection = builder.Build();
            }
            catch (Exception ex)
            {
                s_logger.Debug(ex, $"Failed to build SignalR hub connection: {ex}");
                throw;
            }

            if (timeout != null)
            {
                connection.ServerTimeout = (TimeSpan) timeout;
            }

            connection.Reconnecting += async (ex) =>
            {
                s_logger.Debug(ex, $"Connection reconnecting due to exception: {ex}");
                OnStateChange(HubClientState.Reconnecting);
                await Task.CompletedTask;
            };

            connection.Reconnected += async (connectionId) =>
            {
                s_logger.Debug($"Connection reconnected: New connection ID is {connectionId}");
                await UpdateConnectionId(connectionId);
                OnStateChange(HubClientState.Reconnected);
                await Task.CompletedTask;
            };

#if BROKEN_RETRY_CODE
            connection.Closed += async (ex) =>
            {
                var why = (ex != null) ? $"due to exception: {ex}" : "normally.";
                s_logger.Debug($"Connection closed {why}");
                OnStateChange(HubClientState.Disconnected);

                await Task.Delay(new Random().Next(0,500) * 10);
                OnStateChange(HubClientState.Reconnecting);
                var newState = HubClientState.Disconnected;  // Until proven otherwise below.

                try
                {
                    await connection.StartAsync();
                    if (connection.State == HubConnectionState.Connected)
                    {
                        if (m_connectionId != connection.ConnectionId)
                        {
                            // Update this on the server before marking ourselves as reconnected.
                            await UpdateConnectionId(connection.ConnectionId);
                        }
                        newState = HubClientState.Reconnected;
                    }
                }
                catch (Exception ex2)
                {
                    ConnectException = ex2;
                    s_logger.Debug($"Connect: Caught exception: {ex2.Message}");
                }
                OnStateChange(newState);
            };
#else
            connection.Closed += async (ex) =>
            {
                var why = (ex != null) ? $"due to exception: {ex}" : "normally.";
                s_logger.Debug($"Connection closed {why}");
                OnStateChange(HubClientState.Disconnected);
                await Task.CompletedTask; // Suppress compiler warning.
            };
#endif

            return connection;
        }


        private void RegisterHubCallbacks()
        {
            Connection.On<GameInviteDto>                    ("OnInviteAdded",     (invite)                           => OnInviteAdded     (Mapper.Map<GameInvite>(invite)));
            Connection.On<GameInviteDto>                    ("OnInviteRemoved",   (invite)                           => OnInviteRemoved   (Mapper.Map<GameInvite>(invite)));
            Connection.On<GamePrototypeDto, HubGameTypeDto> ("OnGameAdded",       (prototype, gameType)              => OnGameAdded       (Mapper.Map<GamePrototype>(prototype), Mapper.Map<HubGameType>(gameType)));
            Connection.On<Guid>                             ("OnGameRemoved",     (gameId)                           => OnGameRemoved     (gameId));
            Connection.On<GamePrototypeDto>                 ("OnGameCreated",     (prototype)                        => OnGameCreated     (Mapper.Map<GamePrototype>(prototype)));
            Connection.On<Guid>                             ("OnGameStarted",     (gameId)                           => OnGameStarted     (gameId));
            Connection.On<Guid>                             ("OnGameCompleted",   (gameId)                           => OnGameCompleted   (gameId));
            Connection.On<GamePrototypeDto, string>         ("OnKibitzerAdded",   (prototype, hubUserName)           => OnKibitzerAdded   (Mapper.Map<GamePrototype>(prototype), hubUserName));
            Connection.On<GamePrototypeDto, string>         ("OnKibitzerRemoved", (prototype, hubUserName)           => OnKibitzerRemoved (Mapper.Map<GamePrototype>(prototype), hubUserName));
            Connection.On<Guid, string>                     ("OnGameAbandoned",   (gameId, abandonerName)            => OnGameAbandoned   (gameId, abandonerName));
            Connection.On<Guid, string, string, string>     ("OnChatMessage",     (gameId, sender, target, message)  => OnChatMessage     (gameId, sender, target, message));

            Connection.On<Guid, int, int>                   ("OnTurnStarted",     (gameId, turn, playerId)           => HubGame.OnTurnStarted    (GetGame(gameId), turn, playerId));
            Connection.On<Guid, int, int>                   ("OnTurnCompleted",   (gameId, turn, playerId)           => HubGame.OnTurnCompleted  (GetGame(gameId), turn, playerId));
            Connection.On<Guid, StoneMoveDto, int>          ("OnStoneDrawn",      (gameId, move, playerId)           => HubGame.OnStoneDrawn     (GetGame(gameId), Mapper.Map<StoneMove>(move), playerId));
            Connection.On<Guid, StoneMoveDto, int>          ("OnStoneReturned",   (gameId, move, playerId)           => HubGame.OnStoneReturned  (GetGame(gameId), Mapper.Map<StoneMove>(move), playerId));
            Connection.On<Guid, StoneMoveDto, int>          ("OnStonePlaced",     (gameId, move, playerId)           => HubGame.OnStonePlaced    (GetGame(gameId), Mapper.Map<StoneMove>(move), playerId));
            Connection.On<Guid, StackMoveDto, int>          ("OnStackGrabbed",    (gameId, move, playerId)           => HubGame.OnStackGrabbed   (GetGame(gameId), Mapper.Map<StackMove>(move), playerId));
            Connection.On<Guid, StackMoveDto, int>          ("OnStackDropped",    (gameId, move, playerId)           => HubGame.OnStackDropped   (GetGame(gameId), Mapper.Map<StackMove>(move), playerId));
            Connection.On<Guid, MoveDto, int>               ("OnMoveAborted",     (gameId, move, playerId)           => HubGame.OnMoveAborted    (GetGame(gameId), Mapper.Map<IMove>(move), playerId));
            Connection.On<Guid, MoveDto, int>               ("OnMoveMade",        (gameId, move, playerId)           => HubGame.OnMoveMade       (GetGame(gameId), Mapper.Map<IMove>(move), playerId));
            Connection.On<Guid, MoveDto, int, int>          ("OnAbortInitiated",  (gameId, move, playerId, duration) => HubGame.OnAbortInitiated (GetGame(gameId), Mapper.Map<IMove>(move), playerId, duration));
            Connection.On<Guid, MoveDto, int>               ("OnAbortCompleted",  (gameId, move, playerId)           => HubGame.OnAbortCompleted (GetGame(gameId), Mapper.Map<IMove>(move), playerId));
            Connection.On<Guid, MoveDto, int, int>          ("OnMoveInitiated",   (gameId, move, playerId, duration) => HubGame.OnMoveInitiated  (GetGame(gameId), Mapper.Map<IMove>(move), playerId, duration));
            Connection.On<Guid, MoveDto, int>               ("OnMoveCompleted",   (gameId, move, playerId)           => HubGame.OnMoveCompleted  (GetGame(gameId), Mapper.Map<IMove>(move), playerId));
            Connection.On<Guid, MoveDto, int, int>          ("OnUndoInitiated",   (gameId, move, playerId, duration) => HubGame.OnUndoInitiated  (GetGame(gameId), Mapper.Map<IMove>(move), playerId, duration));
            Connection.On<Guid, MoveDto, int>               ("OnUndoCompleted",   (gameId, move, playerId)           => HubGame.OnUndoCompleted  (GetGame(gameId), Mapper.Map<IMove>(move), playerId));
            Connection.On<Guid, MoveDto, int, int>          ("OnRedoInitiated",   (gameId, move, playerId, duration) => HubGame.OnRedoInitiated  (GetGame(gameId), Mapper.Map<IMove>(move), playerId, duration));
            Connection.On<Guid, MoveDto, int>               ("OnRedoCompleted",   (gameId, move, playerId)           => HubGame.OnRedoCompleted  (GetGame(gameId), Mapper.Map<IMove>(move), playerId));
            Connection.On<Guid, BoardPositionDto>           ("OnMoveTracked",     (gameId, pos)                      => HubGame.OnMoveTracked    (GetGame(gameId), Mapper.Map<BoardPosition>(pos)));
            Connection.On<Guid, int, StoneDto[]>            ("OnCurrentTurnSet",  (gameId, turn, stones)             => HubGame.OnCurrentTurnSet (GetGame(gameId), turn, stones.Select(s => Mapper.Map<Stone>(s)).ToArray()));
        }


        private void UnregisterHubCallbacks()
        {
            Connection.Remove("OnInviteAdded");
            Connection.Remove("OnInviteRemoved");
            Connection.Remove("OnGameAdded");
            Connection.Remove("OnGameRemoved");
            Connection.Remove("OnKibitzerAdded");
            Connection.Remove("OnKibitzerRemoved");
            Connection.Remove("OnGameAbandoned");
            Connection.Remove("OnChatMessage");

            Connection.Remove("OnGameCreated");
            Connection.Remove("OnGameStarted");
            Connection.Remove("OnGameCompleted");
            Connection.Remove("OnStoneDrawn");
            Connection.Remove("OnStoneReturned");
            Connection.Remove("OnStonePlaced");
            Connection.Remove("OnStackGrabbed");
            Connection.Remove("OnStackDropped");
            Connection.Remove("OnMoveAborted");
            Connection.Remove("OnMoveMade");
            Connection.Remove("OnAbortInitiated");
            Connection.Remove("OnAbortCompleted");
            Connection.Remove("OnMoveInitiated");
            Connection.Remove("OnMoveCompleted");
            Connection.Remove("OnUndoInitiated");
            Connection.Remove("OnUndoCompleted");
            Connection.Remove("OnRedoInitiated");
            Connection.Remove("OnRedoCompleted");
            Connection.Remove("OnMoveTracked");
            Connection.Remove("OnCurrentTurnSet");
        }
    }
}
