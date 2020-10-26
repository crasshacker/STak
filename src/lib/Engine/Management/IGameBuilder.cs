using System;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Management
{
    public interface IGameBuilder
    {
        IGameActivityTracker BuildTracker   (GamePrototype prototype);
        IGame                ConstructGame  (GamePrototype prototype, IGameActivityTracker tracker = null);
        void                 InitializeGame (IGame game);
        void                 DestructGame   (IGame game);
    }
}
