using System;
using STak.TakEngine;
using STak.TakHub.Interop;

namespace STak.TakHub.Client.Trackers
{
    public interface IHubActivityTracker
    {
        void OnInviteAdded(GameInvite invite);
        void OnInviteRemoved(GameInvite invite);
        void OnGameAdded(GamePrototype prototype, HubGameType gameType);
        void OnGameRemoved(Guid gameId);
        void OnGameAbandoned(Guid gameId, string abandonerName);
        void OnKibitzerAdded(GamePrototype prototype, string kibitzerName);
        void OnKibitzerRemoved(GamePrototype prototype, string kibitzerName);
        void OnChatMessage(Guid gameId, string sender, string target, string message);
    }
}
