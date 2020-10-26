using System;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Management
{
    public interface ITrackerBuilder
    {
        IGameActivityTracker            BuildTracker(GamePrototype prototype, bool useMirroringGame, IDispatcher dispatcher);
        EventRaisingGameActivityTracker GetEventRaisingTracker(IGame game);
        MirroringGameActivityTracker    GetMirroringTracker(IGame game);
    }
}
