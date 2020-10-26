using System;
using System.Threading;
using Akka.Actor;
using STak.TakEngine;
using STak.TakEngine.Trackers;
using STak.TakEngine.Management;

namespace STak.TakEngine.Actors
{
    public class ActorGameConstructor : GameConstructor
    {
        private static long m_nameIdNonce;

        private IActorRef m_supervisorActor;


        public ActorGameConstructor()
        {
        }


        public override IGame Construct(GamePrototype prototype, IGameActivityTracker tracker)
        {
            long nameId = Interlocked.Increment(ref m_nameIdNonce);
            string actorName = AkkaSystem.GetGameSupervisorName(nameId);
            Props props = GameSupervisorActor.Props(nameId, prototype, tracker, AkkaSystem.ActorSystemAddress);
            m_supervisorActor = AkkaSystem.TakActorSystem.ActorOf(props, actorName);
            GetChildActorMessage message = new GetChildActorMessage(AkkaSystem.GetGameExecutorName(nameId));
            return new GameActorFacade(m_supervisorActor.Ask<IActorRef>(message).Result, tracker);
        }


        public override void Destruct(IGame game)
        {
            m_supervisorActor.Tell(new ShutdownMessage(), m_supervisorActor);
        }
    }
}
