using System;
using System.Linq;
using STak.TakEngine;
using STak.TakHub.Interop;

namespace STak.TakHub.Client.Trackers
{
    public class HubActivityTracker : IEventBasedHubActivityTracker
    {
        private readonly IDispatcher m_dispatcher;

        public event EventHandler<InviteAddedEventArgs>     InviteAdded;
        public event EventHandler<InviteRemovedEventArgs>   InviteRemoved;
        public event EventHandler<GameAddedEventArgs>       GameAdded;
        public event EventHandler<GameRemovedEventArgs>     GameRemoved;
        public event EventHandler<KibitzerAddedEventArgs>   KibitzerAdded;
        public event EventHandler<KibitzerRemovedEventArgs> KibitzerRemoved;
        public event EventHandler<GameAbandonedEventArgs>   GameAbandoned;
        public event EventHandler<ChatMessageEventArgs>     ChatMessage;


        public HubActivityTracker(IDispatcher dispatcher)
        {
            m_dispatcher = dispatcher;
        }


        public void OnInviteAdded(GameInvite invite)
        {
            m_dispatcher.Invoke(() => InviteAdded?.Invoke(this, new InviteAddedEventArgs(invite)));
        }


        public void OnInviteRemoved(GameInvite invite)
        {
            m_dispatcher.Invoke(() => InviteRemoved.Invoke(this, new InviteRemovedEventArgs(invite)));
        }


        public void OnGameAdded(GamePrototype prototype, HubGameType gameType)
        {
            m_dispatcher.Invoke(() => GameAdded?.Invoke(this, new GameAddedEventArgs(prototype, gameType)));
        }


        public void OnGameRemoved(Guid gameId)
        {
            m_dispatcher.Invoke(() => GameRemoved?.Invoke(this, new GameRemovedEventArgs(gameId)));
        }


        public void OnGameAbandoned(Guid gameId, string abandonerName)
        {
            m_dispatcher.Invoke(() => GameAbandoned?.Invoke(this, new GameAbandonedEventArgs(gameId, abandonerName)));
        }


        public void OnKibitzerAdded(GamePrototype prototype, string kibitzerName)
        {
            m_dispatcher.Invoke(() => KibitzerAdded?.Invoke(this, new KibitzerAddedEventArgs(prototype, kibitzerName)));
        }


        public void OnKibitzerRemoved(GamePrototype prototype, string kibitzerName)
        {
            m_dispatcher.Invoke(() => KibitzerRemoved?.Invoke(this, new KibitzerRemovedEventArgs(prototype, kibitzerName)));
        }


        public void OnChatMessage(Guid gameId, string sender, string target, string message)
        {
            m_dispatcher.Invoke(() => ChatMessage?.Invoke(this, new ChatMessageEventArgs(gameId, sender, target, message)));
        }
    }
}
