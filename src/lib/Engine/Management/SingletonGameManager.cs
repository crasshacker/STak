using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Management
{
    public class SingletonGameManager : GameManager
    {
        public SingletonGameManager(IGameBuilder gameBuilder = null)
            : base(gameBuilder)
        {
        }


        public override IGame CreateGame(IGameBuilder gameBuilder, GamePrototype prototype)
        {
            foreach (var gameId in m_gameInfo.Keys)
            {
                DestroyGame(gameId);
            }
            return base.CreateGame(gameBuilder, prototype);
        }
    }
}
