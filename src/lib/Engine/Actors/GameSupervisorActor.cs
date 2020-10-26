using System;
using Akka.Actor;
using STak.TakEngine;
using STak.TakEngine.Trackers;

namespace STak.TakEngine.Actors
{
    public class GameSupervisorActor : ReceiveActor
    {
        public string Name { get => AkkaSystem.GetGameName(NameId); }

        public long                 NameId             { get; private set; }
        public IGameActivityTracker Tracker            { get; private set; }
        public GamePrototype        Prototype          { get; private set; }
        public IActorRef            ExecutorActor      { get; private set; }
        public IActorRef            NotifierActor      { get; private set; }
        public string               ActorSystemAddress { get; private set; }


        public GameSupervisorActor(long nameId, GamePrototype prototype, IGameActivityTracker tracker,
                                                                     string actorSystemAddress = null)
        {
            NameId             = nameId;
            Prototype          = prototype;
            Tracker            = tracker;
            ActorSystemAddress = actorSystemAddress;

            Receive<GetChildActorMessage> ((m) => Sender.Tell(Context.Child(m.Name), Self));
            Receive<ShutdownMessage>      ((m) => Context.Stop(Self));
        }


        public static Props Props(GameSupervisorActor actor)
        {
            return Akka.Actor.Props.Create(() => actor);
        }


        public static Props Props(long nameId, GamePrototype prototype, IGameActivityTracker tracker,
                                                                    string actorSystemAddress = null)
        {
            return Akka.Actor.Props.Create(() => new GameSupervisorActor(nameId, prototype, tracker, actorSystemAddress));
        }


        protected override void PreStart()
        {
            CreateGameActors();
        }


        private void CreateGameActors()
        {
            string executorActorName = AkkaSystem.GetGameExecutorName(NameId);
            string notifierActorName = AkkaSystem.GetGameNotifierName(NameId);

            NotifierActor = Context.ActorOf(GameNotifierActor.Props(Tracker), notifierActorName);
            ExecutorActor = Context.ActorOf(GameExecutorActor.Props(Prototype, NotifierActor, ActorSystemAddress),
                                                                                               executorActorName);

            Context.Watch(NotifierActor);
            Context.Watch(ExecutorActor);
        }
    }


    public class GetChildActorMessage
    {
        public string Name { get; private set; }

        public GetChildActorMessage(string name)
        {
            Name = name;
        }
    }


    public class ShutdownMessage
    {
        public ShutdownMessage()
        {
        }
    }
}
