using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Management
{
    public class GameManager
    {
        protected readonly ConcurrentDictionary<Guid, ManagedGameInfo> m_gameInfo;


        public GameManager(IGameBuilder builder = null)
        {
            m_gameInfo = new ConcurrentDictionary<Guid, ManagedGameInfo>();
        }


        public virtual IGame CreateGame(IGameBuilder gameBuilder, GamePrototype prototype)
        {
            IGame game = gameBuilder.ConstructGame(prototype);
            var gameInfo = new ManagedGameInfo(gameBuilder, game);
            m_gameInfo[prototype.Id] = gameInfo;
            gameInfo.Builder.InitializeGame(game);
            return game;
        }


        public List<IGame> GetGames()
        {
            return m_gameInfo.Values.Select(i => i.Game).ToList<IGame>();
        }


        public IGame GetGame(Guid gameId)
        {
            return GetGameInfo(gameId)?.Game;
        }


        public void DestroyGame(Guid gameId)
        {
            ManagedGameInfo info = GetGameInfo(gameId);

            // Don't panic if we didn't find the game; it might have been a game that was "stolen"
            // from the GameManager without informing it (see MainWindow.DeactivateGame).
            if (info != null)
            {
                info.Builder.DestructGame(info.Game);
                m_gameInfo.TryRemove(gameId, out _);
            }
        }


        protected ManagedGameInfo GetGameInfo(Guid gameId)
        {
            m_gameInfo.TryGetValue(gameId, out ManagedGameInfo info);
            return info;
        }


        protected class ManagedGameInfo
        {
            public IGameBuilder Builder { get; set; }
            public IGame       Game     { get; set; }

            public ManagedGameInfo(IGameBuilder builder, IGame game)
            {
                Builder = builder;
                Game    = game;
            }
        }
    }
}
