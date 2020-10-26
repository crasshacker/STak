using System;
using System.Threading.Tasks;
using STak.TakHub.Interop;

namespace STak.TakHub.Core.Interfaces.Hubs
{
    public interface IGameHubContext
    {
        IGameHubClient GetUser   (string userName  );
        IGameHubClient GetClient (string clientName);
        IGameHubClient GetGroup  (string groupName );
        IGameHubClient GetAll    (                 );

        Task AddToGroup      (string groupName, string userOrGroupName);
        Task RemoveFromGroup (string groupName, string userOrGroupName);
    }
}
