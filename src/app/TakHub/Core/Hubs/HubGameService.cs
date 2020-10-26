using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using NLog;
using STak.TakHub.Core.Domain.Entities;
using STak.TakHub.Core.Interfaces.Hubs;
using STak.TakHub.Interop;
using STak.TakEngine;
using STak.TakEngine.AI;
using STak.TakEngine.Actors;
using STak.TakEngine.Trackers;
using STak.TakEngine.Management;

namespace STak.TakHub.Core.Hubs
{
    public class HubGameService : IHubGameService
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        // These can be used to test using simulated latency.
        private static readonly int MinLatency = 0;
        private static readonly int MaxLatency = 0;

        private readonly ConcurrentSet<GameInvite> m_invites;
        private readonly ConcurrentSet<GameTable>  m_tables;
        private readonly ConcurrentSet<Attendee>   m_attendees;
        private readonly GameManager               m_gameManager;
        private readonly HubActivityNotifier       m_notifier;
        private readonly IGameHubContext           m_hubContext;
        private readonly bool                      m_useActorSystem;
        private int                                m_aiConnectionIdNonce;


        public HubGameService(IGameHubContext hubContext, GameManager gameManager,
                                        IOptions<TakHubFrameworkSettings> options)
        {
            m_hubContext  = hubContext;
            m_gameManager = gameManager;
            m_notifier    = new HubActivityNotifier(hubContext);
            m_tables      = new ConcurrentSet<GameTable>();
            m_invites     = new ConcurrentSet<GameInvite>();
            m_attendees   = new ConcurrentSet<Attendee>();

            m_useActorSystem = (bool) options.Value.UseActorSystem;

            // Have AI players to use the animation speed specified by their opponent.
            Player.MoveAnimationTime = (p) => GetMoveAnimationTime(p);
            InitializeAIs();
        }


        public void RegisterConnection(Attendee attendee)
        {
            m_attendees.Add(attendee);
        }


        public void UnregisterConnection(Attendee attendee)
        {
            m_attendees.Remove(attendee);
        }


        public void UpdateConnectionId(string oldId, string newId)
        {
            foreach (var invite in m_invites)
            {
                if (invite.ConnectionId == oldId)
                {
                    invite.ConnectionId = newId;
                }
            }
            foreach (var attendee in m_attendees)
            {
                if (attendee.ConnectionId == oldId)
                {
                    attendee.UpdateConnectionId(newId);
                }
            }
        }


        public void SetProperty(Attendee attendee, string name, string value)
        {
            if (String.Equals(name, "MoveAnimationTime", StringComparison.OrdinalIgnoreCase))
            {
                Int32.TryParse(value, out int speed);
                attendee.MoveAnimationTime = Math.Max(speed, 1);
            }
            else
            {
                throw new Exception($"Value cannot be set for unknown property \"{name}\".");
            }
        }


        public Attendee GetAttendee(Player player)
        {
            return m_attendees.Where(a => a.UserName == player.Name).SingleOrDefault();
        }


        public Attendee GetAttendee(string userName)
        {
            return m_attendees.Where(a => a.UserName == userName).SingleOrDefault();
        }


        public Attendee GetAttendeeForConnection(string connectionId)
        {
            return m_attendees.Where(a => a.ConnectionId == connectionId).SingleOrDefault();
        }


        public GameTable GetTable(Guid gameId)
        {
            return m_tables.Where(t => t.Game.Id == gameId).SingleOrDefault();
        }


        public GameTable GetTableForPlayer(Attendee attendee)
        {
            return GetTableForPlayer(attendee, Guid.Empty);
        }


        public GameTable GetTableForPlayer(Attendee attendee, Guid gameId)
        {
            return m_tables.Where(t => (t.HasPlayer(attendee) && (gameId == Guid.Empty || t.Game.Id == gameId)))
                                                                                              .SingleOrDefault();
        }


        public GameTable GetTableForKibitzer(Attendee attendee)
        {
            return m_tables.Where(t => t.Kibitzers.Where(k => k == attendee).Any()).SingleOrDefault();
        }


        public GameTable GetTableForAttendee(Attendee attendee)
        {
            return GetTableForPlayer(attendee) ?? GetTableForKibitzer(attendee);
        }


        public async Task RequestActiveInvites(Attendee attendee)
        {
            foreach (GameInvite invite in CopyInviteList())
            {
             // FIXIT ...
                await m_notifier.NotifyInviteAdded(invite, m_hubContext.GetClient(attendee.ConnectionId));
            }
        }


        public async Task RequestActiveGames(Attendee attendee)
        {
            foreach (var prototype in m_tables.Select(t => t.Game.Prototype))
            {
                var gameType = new HubGameType(HubGameType.Any);
                await m_notifier.NotifyGameAdded(prototype, gameType, m_hubContext.GetClient(attendee.ConnectionId));
            }
        }


        public async Task InviteGame(GameInvite invite)
        {
            GameInvite matchingInvite = null;

            // Discard any existing invites and/or game associated with the player.
            // FIXIT? - Do we really want to force the user to quit their game upon posting the invite,
            //          rather than when the invite is accepted?
            var attendee = GetAttendeeForConnection(invite.ConnectionId);
            await QuitGame(attendee, Guid.Empty);
            RemovePlayerInvites(attendee);

            List<GameInvite> invites = FindMatchingInvites(invite, CopyInviteList()).ToList<GameInvite>();

            if (invites.Count == 0)
            {
                m_invites.Add(invite);

                // Notify all clients of the new invite.
                await m_notifier.NotifyInviteAdded(invite);
            }
            else
            {
                matchingInvite = invites[0];
                m_invites.Remove(matchingInvite);

                // Notify all clients of removal of the game.
                await m_notifier.NotifyInviteRemoved(matchingInvite);
            }

            if (matchingInvite != null)
            {
                //
                // We pass in the invite that's been waiting for an opponent as the first argument;
                // since this player has been good enough to wait for a suitable opponent to show up
                // she will be given the PlayerOne position if the invite matching algorithm allows.
                //
                var table = await CreateGameTable(matchingInvite, invite);
                m_tables.Add(table);

                // Accept the invite on behalf of the AI player, if there is one.
                if (table.Game.PlayerOne.IsAI) { AcceptGame(table.Player1Id); }
                if (table.Game.PlayerTwo.IsAI) { AcceptGame(table.Player2Id); }

                // Resolve to a game type that should be acceptable to both players.
                var gameType = HubGameType.Resolve(invite.GameType, matchingInvite.GameType);

                // Notify all hub users that a new game is active.
                await m_notifier.NotifyGameAdded(table.Game.Prototype, gameType);
            }
        }


        public async Task KibitzGame(Attendee kibitzer, Guid gameId)
        {
            GameTable table = null;

            // TODO - We need to implement a KibitzerAdded callback (to the user only, or the table, or all?).
            //        The call should pass the GamePrototype to the client, who should execute the associated
            //        moves.  (What if additional moves are made before the client gets listeners hooked up?
            //        Lots of design needed here.)

            table = m_tables.Where(t => t.Game.Id == gameId).SingleOrDefault();

            if (table?.GameType.IsPublic == true)
            {
                await AddKibitzerToTable(table, kibitzer);
            }
        }


        public async Task Chat(Attendee attendee, Guid gameId, string target, string message)
        {
            var clients = GetChatGroup(attendee, target);

            string targetName = (clients.Length > 0) ? target : "no one present";
            s_logger.Debug($"{attendee.UserName} is chatting to {targetName}.");

            foreach (var client in clients)
            {
                await m_notifier.NotifyChatMessage(gameId, attendee.UserName, target, message, client);
            }
        }


        public void AcceptGame(string connectionId)
        {
            GameTable table = m_tables.Where(t => t.HasPlayerForConnection(connectionId)).SingleOrDefault();

            if (table == null)
            {
                throw new Exception("Attempt to accept nonexistent game invitation.");
            }

            if (table.Player1Id == connectionId)
            {
                if (table.P1Accepted)
                {
                    throw new Exception("Cannot accept an invitation that has already been accepted.");
                }
                table.P1Accepted = true;
            }
            else if (table.Player2Id == connectionId)
            {
                if (table.P2Accepted)
                {
                    throw new Exception("Cannot accept an invitation that has already been accepted.");
                }
                table.P2Accepted = true;
            }

            if (table.P1Accepted && table.P2Accepted)
            {
                StartGame(table.Game);
            }
        }


        private int GetMoveAnimationTime(Player player)
        {
            var table = m_tables.Where((t) => t.Game.PlayerOne.Name == player.Name
                                           || t.Game.PlayerTwo.Name == player.Name)
                                                                 .SingleOrDefault();
            var a1 = table.PlayerOne;
            var a2 = table.PlayerTwo;

            var p1 = table.Game.PlayerOne;
            var p2 = table.Game.PlayerTwo;

            return (player.Name == p1.Name && p1.IsHuman) ? a1.MoveAnimationTime
                 : (player.Name == p2.Name && p2.IsHuman) ? a2.MoveAnimationTime
                 : (player.Name == p1.Name && p1.IsAI)    ? a2.MoveAnimationTime
                 : (player.Name == p2.Name && p2.IsAI)    ? a1.MoveAnimationTime
                                                          : 1000;
        }


        private IGameHubClient[] GetChatGroup(Attendee attendee, string target)
        {
            List<IGameHubClient> clients = new List<IGameHubClient>();

            if (target == "ALL")
            {
                clients.Add(m_hubContext.GetAll());
            }
            else if (target == "TABLE")
            {
                var table = GetTableForAttendee(attendee);
                if (table == null)
                {
                    // Always include the sender in the list of targets.
                    clients.Add(m_hubContext.GetUser(attendee.UserName));
                }
                else
                {
                    string groupName = table.Game.Id.ToString();
                    clients.Add(m_hubContext.GetGroup(groupName));
                }
            }
            else
            {
                // Always include the sender in the list of targets.
                clients.Add(m_hubContext.GetUser(attendee.UserName));
                IEnumerable<string> userNames = target.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                          .Where(u => u != attendee.UserName).Distinct();
                foreach (string userName in userNames)
                {
                    if (GetAttendee(userName) != null)
                    {
                        clients.Add(m_hubContext.GetUser(userName));
                    }
                }
            }

            return clients.ToArray();
        }


        private void InitializeAIs()
        {
            foreach (string name in TakAI.GetAINames())
            {
                var ai      = TakAI.GetAI(name);
                var options = AIConfiguration<TakAIOptions>.Get(name);

                TakAI.GetAI(name).Options = options;

                for (int i = 0; i < options.MaximumInstanceCount; ++i)
                {
                    m_invites.Add(CreateAIInvite(ai));
                }
            }
        }


        private void StartGame(IGame game)
        {
            game.Start();
        }


        public async Task QuitGame(Attendee attendee, GameTable table)
        {
            if (table != null)
            {
                s_logger.Info("Player {0} is quitting game with ID={1}.", attendee.UserName, table.Game.Id);

                if (table.HasPlayer(attendee))
                {
                    if (! table.HasPlayerDetached(attendee))
                    {
                        await DetachPlayerFromGame(table, attendee);
                    }

                    // TODO - If this is the first player to detach, allow the game to live on with the
                    //        other player and kibitzers still active, so they can chat and review the game.
                    await CloseGameTable(table);
                }
                else
                {
                    // The person quitting the game must be a kibitzer rather than a player.
                    // TODO - Kibitzers are not yet supported.
                }
            }
        }


        public async Task QuitGame(Attendee attendee, Guid gameId)
        {
            var table = GetTableForPlayer(attendee, gameId);

            if (table != null)
            {
                await QuitGame(attendee, GetTableForPlayer(attendee, gameId));
            }
            else if (gameId != Guid.Empty)
            {
                s_logger.Warn("Attempt to quit nonexistent game with ID={0}.", gameId);
            }
        }


        private async Task CloseGameTable(GameTable table)
        {
            s_logger.Debug("Closing game table.");

            IGame game = table.Game;

            // If an AI was being used, make one (of the same type, with a new name) available for use.
            if (game.PlayerOne.IsAI) { await m_notifier.NotifyInviteAdded(CreateAIInvite(game.PlayerOne.AI)); }
            if (game.PlayerTwo.IsAI) { await m_notifier.NotifyInviteAdded(CreateAIInvite(game.PlayerTwo.AI)); }

            await DetachPlayerFromGame(table, table.PlayerOne);
            await DetachPlayerFromGame(table, table.PlayerTwo);

            while (table.Kibitzers.Any())
            {
                await RemoveKibitzerFromTable(table, table.Kibitzers.First());
            }

            // Notify all clients of removal of the game.
            await m_notifier.NotifyGameRemoved(game.Id);

            // Do this AFTER sending all client notifications.
            foreach (var person in table.Attendees)
            {
                // Notifications are done; now remove the players from the group associated with the game.
                s_logger.Info($"Removing attendee {person.UserName} from the table group.");
                await m_hubContext.RemoveFromGroup(person.ConnectionId, game.Id.ToString());
            }

            EventRaisingGameActivityTracker tracker = new TrackerBuilder(m_hubContext).GetEventRaisingTracker(game);

            if (game.PlayerOne.IsAI) { game.PlayerOne.Unobserve(tracker); }
            if (game.PlayerTwo.IsAI) { game.PlayerTwo.Unobserve(tracker); }

            // Make any AI opponents that were in use in the game available for use by other players.
            if (game.PlayerOne.IsAI) { m_invites.Add(CreateAIInvite(game.PlayerOne.AI)); }
            if (game.PlayerTwo.IsAI) { m_invites.Add(CreateAIInvite(game.PlayerTwo.AI)); }

            // Remove and destroy the game.
            m_tables.Remove(table);
            m_gameManager.DestroyGame(game.Id);

            s_logger.Debug("Finished closing game table.");
        }


        private async Task DetachPlayerFromGame(GameTable table, Attendee player)
        {
            bool notifyAbandoned = false;
            bool isHuman         = false;

            if (player == table.PlayerOne && ! table.P1Detached)
            {
                isHuman = table.Game.PlayerOne.IsHuman;
                table.P1Detached = true;
                notifyAbandoned  = true;
            }
            if (player == table.PlayerTwo && ! table.P2Detached)
            {
                isHuman = table.Game.PlayerTwo.IsHuman;
                table.P2Detached = true;
                notifyAbandoned  = true;
            }

            if (notifyAbandoned && isHuman)
            {
                var group = m_hubContext.GetGroup(table.Game.Id.ToString());
                await m_notifier.NotifyGameAbandoned(table.Game.Id, player.UserName, group);
            }
        }


        public void HandleAttendeeConnection(string login, string connectionId)
        {
            RegisterConnection(CreateAttendeeForLogin(login, connectionId));
        }


        public async Task HandleAttendeeDisconnection(string connectionId)
        {
            var attendee = m_attendees.Where(a => a.ConnectionId == connectionId).SingleOrDefault();

            if (attendee != null)
            {
                var table = GetTableForAttendee(attendee);

                if (table != null)
                {
                    if (table.HasPlayer(attendee))
                    {
                        await QuitGame(attendee, table);
                    }
                    else
                    {
                        await RemoveKibitzerFromTable(table, attendee);
                    }
                }

                RemovePlayerInvites(attendee);
                UnregisterConnection(attendee);
            }
        }


        private static IEnumerable<GameInvite> FindMatchingInvites(GameInvite newInvite,
                                                 IEnumerable<GameInvite> pendingInvites)
        {
            return from pendingInvite in pendingInvites
                   where GameInvite.IsMatch(newInvite, pendingInvite)
                   orderby pendingInvite.CreateTime
                   select pendingInvite;
        }


        private GameInvite CreateAIInvite(ITakAI takAI)
        {
            GameInvite invite = new GameInvite();
            Interlocked.Increment(ref m_aiConnectionIdNonce);
            string aiIdentifier = String.Format("AI-{0}", m_aiConnectionIdNonce);
            RegisterConnection(new Attendee(aiIdentifier, aiIdentifier));
            invite.Inviter = takAI.Name;
            invite.ConnectionId = aiIdentifier;
            invite.IsInviterAI = true;
            invite.WillPlayAI = true;
            return invite;
        }


        private async Task<GameTable> CreateGameTable(GameInvite invite1, GameInvite invite2)
        {
            // Make invite1 PlayerOne if she wants that position, unless it would violate invite2's requirements.
            bool invite1IsPlayerOne = (! invite1.PlayerNumber.Any() || invite1.PlayerNumber.Contains(Player.One))
                                   && (! invite2.PlayerNumber.Any() || invite2.PlayerNumber.Contains(Player.Two));

            string player1Id = invite1IsPlayerOne ? invite1.ConnectionId  : invite2.ConnectionId;
            string player2Id = invite1IsPlayerOne ? invite2.ConnectionId  : invite1.ConnectionId;
            Player player1   = invite1IsPlayerOne ? CreatePlayer(invite1) : CreatePlayer(invite2);
            Player player2   = invite1IsPlayerOne ? CreatePlayer(invite2) : CreatePlayer(invite1);

            int boardSize = Board.Sizes.First(s => (invite1.BoardSize.Contains(s) || ! invite1.BoardSize.Any())
                                                && (invite2.BoardSize.Contains(s) || ! invite2.BoardSize.Any()));

            GamePrototype prototype = new GamePrototype(player1, player2, boardSize);
            string groupName = prototype.Id.ToString();

            s_logger.Info("Creating game with ID={0}.", groupName);
            s_logger.Info("Adding player with ID={0} to the table group.", player1Id);
            s_logger.Info("Adding player with ID={0} to the table group.", player2Id);

            if ((invite1.TimeLimit.Max < Int32.MaxValue || invite2.TimeLimit.Max < Int32.MaxValue)
             && (invite1.Increment.Max < Int32.MaxValue || invite2.Increment.Max < Int32.MaxValue))
            {
                //
                // This determination of the appropriate time limit and increment could be improved.  Currently we just
                // use invite1's time limit range average if they specified a limit, otherwise we use invite2's average
                // limit if they specified one, otherwise the time limit is unlimited (no game clock).  The same goes
                // for the per-ply time increment used in timed games.
                //
                var timeLimit = (invite1.TimeLimit.Max < Int32.MaxValue)
                              ? (invite1.TimeLimit.Max + invite1.TimeLimit.Min) / 2
                              : (invite2.TimeLimit.Max + invite2.TimeLimit.Min) / 2;

                var increment = (invite1.Increment.Max < Int32.MaxValue)
                              ? (invite1.Increment.Max + invite1.Increment.Min) / 2
                              : (invite2.Increment.Max + invite2.Increment.Min) / 2;

                prototype.GameTimer = new GameTimer(TimeSpan.FromSeconds(timeLimit),
                                                    TimeSpan.FromSeconds(increment));

                s_logger.Info($"The game is time-limited to {timeLimit} + {increment}-per-ply seconds.");
            }

            // Add human players (only) to the group to send game tracking messages to.
            // NOTE: We need to do this prior to creating the game, to ensure the game creation and
            //       initialization messages are sent to the client(s) via the game's tracker.
            if (player1.IsHuman) { await m_hubContext.AddToGroup(player1Id, groupName); }
            if (player2.IsHuman) { await m_hubContext.AddToGroup(player2Id, groupName); }

            var builderType = m_useActorSystem ? GameBuilderType.LocalActor : GameBuilderType.LocalDirect;
            TrackerBuilder trackerBuilder = new TrackerBuilder(m_hubContext);
            GameBuilder gameBuilder = CreateGameBuilder(builderType, trackerBuilder);
            IGame game = m_gameManager.CreateGame(gameBuilder, prototype);

            // Attach computer players (only) directly to the event raising tracker.
            EventRaisingGameActivityTracker eventingTracker = trackerBuilder.GetEventRaisingTracker(game);
            if (game.PlayerOne.IsAI) { game.PlayerOne.Observe(eventingTracker); }
            if (game.PlayerTwo.IsAI) { game.PlayerTwo.Observe(eventingTracker); }

            HubGameType gameType  = HubGameType.GetBestMatch(invite1.GameType, invite2.GameType);
            var attendee1 = GetAttendeeForConnection(player1Id);
            var attendee2 = GetAttendeeForConnection(player2Id);
            return new GameTable(game, gameType, attendee1, attendee2);
        }


        private GameBuilder CreateGameBuilder(GameBuilderType builderType, TrackerBuilder trackerBuilder)
        {
            var builder = builderType switch
            {
                GameBuilderType.LocalDirect => new GameBuilder(trackerBuilder, false),
                GameBuilderType.LocalActor  => new ActorGameBuilder(trackerBuilder),
                _                           => null
            };

            return builder;
        }


        private Attendee CreateAttendeeForLogin(string login, string connectionId)
        {
            string userName = login;

            if (m_attendees.Where(a => a.UserName == userName).Any())
            {
                for (int i = 1; true; ++i)
                {
                    userName = $"{login} [{i}]";
                    if (! m_attendees.Where(a => a.UserName == userName).Any())
                    {
                        break;
                    }
                }
            }

            return new Attendee(userName, connectionId);
        }


        private Player CreatePlayer(GameInvite invite)
        {
            string playerName = invite.Inviter;
            ITakAI computerAI = GetComputerAI(invite);

            // Mark AI players as local and human players as remote.  It is up to the client to modify
            // players' locality to be correct from the client's point of view.  See GameHubClient.cs.
            return (computerAI != null) ? new Player(playerName, computerAI)
                                        : new Player(playerName, true);
        }


        private void RemovePlayerInvites(Attendee attendee)
        {
            if (attendee != null)
            {
                var invites = m_invites.Where(i => i.ConnectionId == attendee.ConnectionId).ToArray();

                for (int i = 0; i < invites.Length; ++i)
                {
                    m_invites.Remove(invites[i]);
                }
            }
        }


        private ITakAI GetComputerAI(GameInvite invite)
        {
            return invite.IsInviterAI ? TakAI.GetAI(invite.Inviter) : null;
        }


        private GameInvite[] CopyInviteList()
        {
            return m_invites.ToArray();
        }


        private async Task AddKibitzerToTable(GameTable table, Attendee attendee)
        {
            lock (table.Kibitzers)
            {
                table.AddKibitzer(attendee);
            }

            var game = table.Game;
            var prototype = new GamePrototype(game.Prototype);

            // Include executed moves in the prototype we're sending, so client can replay them.
            prototype.Moves = new List<IMove>(game.ExecutedMoves.Select(m => m.Clone()));

            if (game.StoneMove != null) prototype.Moves.Add(game.StoneMove);
            if (game.StackMove != null) prototype.Moves.Add(game.StackMove);

            await m_hubContext.AddToGroup(attendee.ConnectionId, game.Id.ToString());
            await m_notifier.NotifyKibitzerAdded(prototype, attendee.UserName);
        }


        private async Task RemoveKibitzerFromTable(GameTable table, Attendee attendee)
        {
            lock (table)
            {
                table.RemoveKibitzer(attendee);
            }

            await m_notifier.NotifyKibitzerRemoved(table.Game.Prototype, attendee.UserName);
            await m_hubContext.RemoveFromGroup(attendee.ConnectionId, table.Game.Id.ToString());
        }


        public class TrackerBuilder : ITrackerBuilder
        {
            private readonly IGameHubContext m_hubContext;


            public TrackerBuilder(IGameHubContext hubContext)
            {
                m_hubContext = hubContext;
            }


            public IGameActivityTracker BuildTracker(GamePrototype prototype, bool useMirroringGame = false,
                                                                              IDispatcher dispatcher = null)
            {
                // NOTE: We currently ignore the mirroring and dispatching options.  Additionally, We could
                //       theoretically use only the HubGameActivityTracker in the case where both players
                //       are remote (no hub-based AI player), but lower level code currently depends on
                //       there being an EventRaisingGameActivityTracker associated with the game.

                // Build a tracker that calls into both remote human players and local AI players.

                var hubGameTracker = new HubGameActivityTracker(m_hubContext, prototype, MinLatency, MaxLatency);
                EventRaisingGameActivityTracker eventingTracker = new EventRaisingGameActivityTracker();
                IGameActivityTracker tracker = eventingTracker;

                if (useMirroringGame)
                {
                    var mirroringTracker = new MirroringGameActivityTracker(prototype, eventingTracker);
                    eventingTracker.Game = mirroringTracker.Game;
                    tracker = mirroringTracker;
                }

                return new FanoutGameActivityTracker(tracker, hubGameTracker);
            }


            public EventRaisingGameActivityTracker GetEventRaisingTracker(IGame game)
            {
                var fanoutTracker = game.Tracker as FanoutGameActivityTracker;
                var eventingTracker = fanoutTracker.GetTracker<EventRaisingGameActivityTracker>();

                if (eventingTracker is null)
                {
                    var mirroringTracker = fanoutTracker.GetTracker<MirroringGameActivityTracker>();
                    eventingTracker = mirroringTracker?.Tracker as EventRaisingGameActivityTracker;
                }

                return eventingTracker;
            }


            public MirroringGameActivityTracker GetMirroringTracker(IGame game)
            {
                return null;
            }
        }
    }
}
