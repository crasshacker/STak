using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using STak.TakHub.Core;
using STak.TakHub.Core.Interfaces.Hubs;
using STak.TakHub.Interop;

namespace STak.TakHub.Hubs
{
    public class SignalRGameHubContext : IGameHubContext
    {
        private readonly IHubContext<GameHub, IGameHubClient> m_hubContext;


        public SignalRGameHubContext(IHubContext<GameHub, IGameHubClient> hubContext)
        {
            m_hubContext = hubContext;
        }


        public IGameHubClient GetUser   (string userName  ) => m_hubContext.Clients.User(userName);
        public IGameHubClient GetClient (string clientName) => m_hubContext.Clients.Client(clientName);
        public IGameHubClient GetGroup  (string groupName ) => m_hubContext.Clients.Group(groupName);
        public IGameHubClient GetAll    (                 ) => m_hubContext.Clients.All;

        public async Task AddToGroup (string clientId, string groupName)
             => await m_hubContext.Groups.AddToGroupAsync(clientId, groupName);

        public async Task RemoveFromGroup (string clientId, string groupName)
             => await m_hubContext.Groups.RemoveFromGroupAsync(clientId, groupName);
    }
}
