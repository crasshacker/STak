using System;
using NLog;
using STak.TakEngine;
using STak.TakEngine.Trackers;
using STak.TakEngine.Management;

namespace STak.TakEngine.Actors
{
    public class ActorGameBuilder : GameBuilder
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();
        private readonly ActorGameConstructor m_constructor;


        public ActorGameBuilder(ITrackerBuilder trackerBuilder, bool mirrorGame = true, IDispatcher dispatcher = null)
            : base(trackerBuilder, mirrorGame, dispatcher)
        {
            m_constructor = new ActorGameConstructor();

            this.GameConstructed += (s, e) => ((GameActorFacade)e.Game).InitializeGameState();
        }


        public override IGame ConstructGame(GamePrototype prototype, IGameActivityTracker tracker = null)
        {
            s_logger.Debug($"Constructing actor-based game: {prototype.Id}");
            var game = m_constructor.Construct(prototype, tracker ?? BuildTracker(prototype));
            ((GameActorFacade)game).InitializeGameState();
            InvokeGameConstructed(new GameConstructedEventArgs(game));
            return game;
        }


        public override void DestructGame (IGame game)
        {
            s_logger.Debug($"Destructing actor-based game: {game.Id}");
            m_constructor.Destruct(game);
            InvokeGameDestructed(new GameDestructedEventArgs(game));
        }
    }
}
