using System;
using STak.TakEngine;

namespace STak.TakHub.Interop
{
    public class HubCommand : TakCommand
    {
        public static readonly HubCommand GetHubUserName       = new HubCommand { Name = "GetHubUserName",       LogExecution = true };
        public static readonly HubCommand SetProperty          = new HubCommand { Name = "SetProperty",          LogExecution = true };
        public static readonly HubCommand RequestActiveInvites = new HubCommand { Name = "RequestActiveInvites", LogExecution = true };
        public static readonly HubCommand RequestActiveGames   = new HubCommand { Name = "RequestActiveGames",   LogExecution = true };
        public static readonly HubCommand InviteGame           = new HubCommand { Name = "InviteGame",           LogExecution = true };
        public static readonly HubCommand KibitzGame           = new HubCommand { Name = "KibitzGame",           LogExecution = true };
        public static readonly HubCommand QuitGame             = new HubCommand { Name = "QuitGame",             LogExecution = true };
        public static readonly HubCommand Chat                 = new HubCommand { Name = "Chat",                 LogExecution = true };

        // *** For internal use only.
        public static readonly HubCommand UpdateConnectionId   = new HubCommand { Name = "UpdateConnectionId",   LogExecution = true };
    }


    public class HubNotification : TakNotification
    {
        public static readonly HubNotification GameCreated     = new HubNotification { Name = "OnGameCreated",     LogExecution = false };
        public static readonly HubNotification GameStarted     = new HubNotification { Name = "OnGameStarted",     LogExecution = false };
        public static readonly HubNotification KibitzerAdded   = new HubNotification { Name = "OnKibitzerAdded",   LogExecution = false };
        public static readonly HubNotification KibitzerRemoved = new HubNotification { Name = "OnKibitzerRemoved", LogExecution = false };
        public static readonly HubNotification GameAbandoned   = new HubNotification { Name = "OnGameAbandoned",   LogExecution = false };
        public static readonly HubNotification InviteAdded     = new HubNotification { Name = "OnInviteAdded",     LogExecution = false };
        public static readonly HubNotification InviteRemoved   = new HubNotification { Name = "OnInviteRemoved",   LogExecution = false };
        public static readonly HubNotification GameAdded       = new HubNotification { Name = "OnGameAdded",       LogExecution = false };
        public static readonly HubNotification GameRemoved     = new HubNotification { Name = "OnGameRemoved",     LogExecution = false };
        public static readonly HubNotification ChatMessage     = new HubNotification { Name = "OnChatMessage",     LogExecution = false };
    }
}
