using System;
using System.Threading.Tasks;
using NLog;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Management
{
    public class GameBuilder : IGameBuilder
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        public bool            MirrorGame     { get; }
        public IDispatcher     Dispatcher     { get; }
        public ITrackerBuilder TrackerBuilder { get; }

        public event EventHandler<GameConstructedEventArgs> GameConstructed;
        public event EventHandler<GameInitializedEventArgs> GameInitialized;
        public event EventHandler<GameDestructedEventArgs>  GameDestructed;

        protected void InvokeGameConstructed(GameConstructedEventArgs e) { GameConstructed?.Invoke(this, e); }
        protected void InvokeGameInitialized(GameInitializedEventArgs e) { GameInitialized?.Invoke(this, e); }
        protected void InvokeGameDestructed (GameDestructedEventArgs  e) { GameDestructed? .Invoke(this, e); }


        public GameBuilder(ITrackerBuilder trackerBuilder, bool mirrorGame = true, IDispatcher dispatcher = null)
        {
            TrackerBuilder = trackerBuilder;
            MirrorGame     = mirrorGame;
            Dispatcher     = dispatcher;
        }


        public virtual IGameActivityTracker BuildTracker(GamePrototype prototype)
        {
            s_logger.Debug($"Building tracker for game: {prototype.Id}");
            return TrackerBuilder.BuildTracker(prototype, MirrorGame, Dispatcher);
        }


        public virtual IGame ConstructGame(GamePrototype prototype, IGameActivityTracker tracker = null)
        {
            s_logger.Debug($"Constructing game: {prototype.Id}");
            var game = new Game(prototype, tracker ?? BuildTracker(prototype));
            TrackerBuilder.GetEventRaisingTracker(game).Game = game;
            InvokeGameConstructed(new GameConstructedEventArgs(game));
            return game;
        }


        public virtual void InitializeGame(IGame game)
        {
            s_logger.Debug($"Initializing game: {game.Id}");
            game.Initialize();
            InvokeGameInitialized(new GameInitializedEventArgs(game));
        }


        public virtual void DestructGame(IGame game)
        {
            s_logger.Debug($"Destructing game: {game.Id}");
            InvokeGameDestructed(new GameDestructedEventArgs(game));
        }
    }
}
