using System;
using STak.TakEngine;

namespace STak.TakHub.Interop
{
    [Serializable]
    public class InviteAddedEventArgs : EventArgs
    {
        public InviteAddedEventArgs(GameInvite invite)
        {
            Invite = invite;
        }

        public GameInvite Invite { get; set; }
    }


    [Serializable]
    public class InviteRemovedEventArgs : EventArgs
    {
        public InviteRemovedEventArgs(GameInvite invite)
        {
            Invite = invite;
        }

        public GameInvite Invite { get; set; }
    }


    [Serializable]
    public class GameAddedEventArgs : EventArgs
    {
        public GameAddedEventArgs(GamePrototype prototype, HubGameType gameType)
        {
            Prototype = prototype;
            GameType  = gameType;
        }

        public GamePrototype Prototype { get; set; }
        public HubGameType   GameType  { get; set; }
    }


    [Serializable]
    public class GameRemovedEventArgs : EventArgs
    {
        public GameRemovedEventArgs(Guid gameId)
        {
            GameId = gameId;
        }

        public Guid GameId { get; set; }
    }


    [Serializable]
    public class KibitzerAddedEventArgs : EventArgs
    {
        public KibitzerAddedEventArgs(GamePrototype prototype, string kibitzerName)
        {
            Prototype    = prototype;
            KibitzerName = kibitzerName;
        }

        public GamePrototype Prototype    { get; set; }
        public string        KibitzerName { get; set; }
    }


    [Serializable]
    public class KibitzerRemovedEventArgs : EventArgs
    {
        public KibitzerRemovedEventArgs(GamePrototype prototype, string kibitzerName)
        {
            Prototype    = prototype;
            KibitzerName = kibitzerName;
        }

        public GamePrototype Prototype    { get; set; }
        public string        KibitzerName { get; set; }
    }


    [Serializable]
    public class GameAbandonedEventArgs : EventArgs
    {
        public GameAbandonedEventArgs(Guid gameId, string abandonerName)
        {
            GameId        = gameId;
            AbandonerName = abandonerName;
        }

        public Guid   GameId        { get; set; }
        public string AbandonerName { get; set; }
    }


    [Serializable]
    public class ChatMessageEventArgs : EventArgs
    {
        public ChatMessageEventArgs(Guid gameId, string sender, string target, string message)
        {
            GameId  = gameId;
            Sender  = sender;
            Target  = target;
            Message = message;
        }

        public Guid   GameId  { get; set; }
        public string Sender  { get; set; }
        public string Target  { get; set; }
        public string Message { get; set; }
    }
}
