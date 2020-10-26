using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using STak.TakHub.Core.Interfaces.Hubs;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.UseCases;
using STak.TakHub.Interop;
using STak.TakHub.Interop.Dto;
using STak.TakEngine;
using NLog;

namespace STak.TakHub.Core.Hubs
{
    public class HubActivityNotifier
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private readonly IGameHubContext m_hubContext;


        public HubActivityNotifier(IGameHubContext hubContext)
        {
            m_hubContext = hubContext;
        }


        public async Task NotifyInviteAdded(GameInvite invite, IGameHubClient clients = null)
        {
            clients ??= m_hubContext.GetAll();
            LogNotification(HubNotification.InviteAdded, invite.Id);
            var inviteDto = Mapper.Map<GameInviteDto>(invite);
            await clients.OnInviteAdded(inviteDto);
        }


        public async Task NotifyInviteRemoved(GameInvite invite, IGameHubClient clients = null)
        {
            clients ??= m_hubContext.GetAll();
            LogNotification(HubNotification.InviteRemoved, invite.Id);
            var inviteDto = Mapper.Map<GameInviteDto>(invite);
            await clients.OnInviteRemoved(inviteDto);
        }


        public async Task NotifyGameAdded(GamePrototype prototype, HubGameType gameType, IGameHubClient clients = null)
        {
            clients ??= m_hubContext.GetAll();
            LogNotification(HubNotification.GameAdded, prototype.Id);
            var prototypeDto = Mapper.Map<GamePrototypeDto>(prototype);
            var gameTypeDto = Mapper.Map<HubGameTypeDto>(gameType);
            await clients.OnGameAdded(prototypeDto, gameTypeDto);
        }


        public async Task NotifyGameRemoved(Guid gameId, IGameHubClient clients = null)
        {
            clients ??= m_hubContext.GetAll();
            LogNotification(HubNotification.GameRemoved, gameId);
            await clients.OnGameRemoved(gameId);
        }


        public async Task NotifyKibitzerAdded(GamePrototype prototype, string kibitzerName, IGameHubClient clients = null)
        {
            clients ??= m_hubContext.GetAll();
            LogNotification(HubNotification.KibitzerAdded, kibitzerName);
            var prototypeDto = Mapper.Map<GamePrototypeDto>(prototype);
            await clients.OnKibitzerAdded(prototypeDto, kibitzerName);
        }


        public async Task NotifyKibitzerRemoved(GamePrototype prototype, string kibitzerName, IGameHubClient clients = null)
        {
            clients ??= m_hubContext.GetAll();
            LogNotification(HubNotification.KibitzerRemoved, kibitzerName);
            var prototypeDto = Mapper.Map<GamePrototypeDto>(prototype);
            await clients.OnKibitzerRemoved(prototypeDto, kibitzerName);
        }


        public async Task NotifyGameAbandoned(Guid gameId, string abandonerName, IGameHubClient clients = null)
        {
            clients ??= m_hubContext.GetAll();
            LogNotification(HubNotification.GameAbandoned, abandonerName);
            await clients.OnGameAbandoned(gameId, abandonerName);
        }


        public async Task NotifyChatMessage(Guid gameId, string sender, string target, string message, IGameHubClient clients = null)
        {
            clients ??= m_hubContext.GetAll();
            LogNotification(HubNotification.ChatMessage, message);
            await clients.OnChatMessage(gameId, sender, target, message);
        }


        private static void LogNotification(HubNotification notification, object arg)
        {
            s_logger.Debug($"Sending {notification.Name} notification (Arg: {arg}).");
        }
    }
}
