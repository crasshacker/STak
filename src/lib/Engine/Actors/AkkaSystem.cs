using System;
using Akka.Actor;
using Akka.Event;
using Akka.Configuration;

namespace STak.TakEngine.Actors
{
    public static class AkkaSystem
    {
        public const string ActorSystemName = "tak";

        public static string      ActorSystemAddress { get; private set; }
        public static ActorSystem TakActorSystem     { get; private set; }


        public static void Initialize(string config = null, string address = null)
        {
            if (TakActorSystem == null)
            {
                TakActorSystem = (config != null) ? ActorSystem.Create(ActorSystemName, config)
                                                  : ActorSystem.Create(ActorSystemName);
            }

            ActorSystemAddress = address;
        }


        public static string GetGameName(long gameId)
        {
            return String.Format("game{0}", gameId);
        }


        public static string GetGameSupervisorName(long gameId)
        {
            return String.Format("game-supervisor{0}", gameId);
        }


        public static string GetGameExecutorName(long gameId)
        {
            return String.Format("game-executor{0}", gameId);
        }


        public static string GetGameNotifierName(long gameId)
        {
            return String.Format("game-notifier{0}", gameId);
        }
    }
}
