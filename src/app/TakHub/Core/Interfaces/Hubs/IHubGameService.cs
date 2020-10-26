using System;
using System.Threading.Tasks;
using STak.TakHub.Core.Hubs;
using STak.TakHub.Interop;

namespace STak.TakHub.Core.Interfaces.Hubs
{
    public interface IHubGameService
    {
        void      SetProperty(Attendee attendee, string name, string value);
        Attendee  GetAttendeeForConnection(string connectionId);
        GameTable GetTableForPlayer(Attendee attendee, Guid gameId);
        GameTable GetTableForKibitzer(Attendee attendee);
        GameTable GetTableForAttendee(Attendee attendee);
        void      UpdateConnectionId(string oldId, string newId);
        void      RegisterConnection(Attendee attendee);
        void      UnregisterConnection(Attendee attendee);
        Task      RequestActiveInvites(Attendee attendee);
        Task      RequestActiveGames(Attendee attendee);
        Task      InviteGame(GameInvite invite);
        Task      KibitzGame(Attendee kibitzer, Guid gameId);
        void      AcceptGame(string playerId);
        Task      QuitGame(Attendee attendee, Guid gameId);
        Task      Chat(Attendee attendee, Guid gameId, string target, string message);
        void      HandleAttendeeConnection(string userName, string connectionId);
        Task      HandleAttendeeDisconnection(string connectionId);
    }
}
