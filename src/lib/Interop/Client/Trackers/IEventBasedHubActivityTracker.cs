using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using NLog;
using STak.TakEngine;
using STak.TakHub.Interop;

namespace STak.TakHub.Client.Trackers
{
    public interface IEventBasedHubActivityTracker : IHubActivityTracker
    {
        event EventHandler<InviteAddedEventArgs>     InviteAdded;
        event EventHandler<InviteRemovedEventArgs>   InviteRemoved;
        event EventHandler<GameAddedEventArgs>       GameAdded;
        event EventHandler<GameRemovedEventArgs>     GameRemoved;
        event EventHandler<KibitzerAddedEventArgs>   KibitzerAdded;
        event EventHandler<KibitzerRemovedEventArgs> KibitzerRemoved;
        event EventHandler<GameAbandonedEventArgs>   GameAbandoned;
        event EventHandler<ChatMessageEventArgs>     ChatMessage;
    }
}
