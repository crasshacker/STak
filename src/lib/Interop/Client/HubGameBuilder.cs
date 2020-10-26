using System;
using NLog;
using STak.TakEngine;
using STak.TakEngine.Trackers;
using STak.TakEngine.Management;

namespace STak.TakHub.Client
{
    public class HubGameBuilder : GameBuilder
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private readonly GameHubClient m_hubClient;


        public HubGameBuilder(ITrackerBuilder trackerBuilder, GameHubClient hubClient, IDispatcher dispatcher = null)
            : base(trackerBuilder, true, dispatcher)
        {
            m_hubClient = hubClient;
        }


        public override IGame ConstructGame(GamePrototype prototype, IGameActivityTracker tracker = null)
        {
            s_logger.Debug($"Constructing remote game: {prototype.Id}");
            var game = new HubGame(m_hubClient, prototype, tracker ?? BuildTracker(prototype));
            InvokeGameConstructed(new GameConstructedEventArgs(game));
            return game;
        }
    }
}
