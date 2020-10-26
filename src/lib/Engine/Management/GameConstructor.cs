using System;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Management
{
    public class GameConstructor
    {
        public GameConstructor()
        {
        }


        public virtual IGame Construct(GamePrototype prototype, IGameActivityTracker tracker)
        {
            return new Game(prototype, tracker);
        }


        public virtual void Destruct(IGame game)
        {
        }
    }
}
