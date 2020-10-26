using System;
using NodaTime;

namespace STak.TakHub.Core.Hubs
{
    public class Attendee
    {
        public string  UserName          { get; }
        public string  ConnectionId      { get; private set; }
        public Instant LastActiveAt      { get; private set; }
        public int     MoveAnimationTime { get;         set; } = 1000;


        public Attendee(string userName, string connectionId)
        {
            UserName     = userName;
            ConnectionId = connectionId;
            LastActiveAt = GetCurrentTime();
        }


        public void UpdateConnectionId(string connectionId)
        {
            ConnectionId = connectionId;
        }


        public Duration GetIdleTime()
        {
            return GetCurrentTime() - LastActiveAt;
        }


        public void UpdateLastActivityTime()
        {
            LastActiveAt = GetCurrentTime();
        }


        private static Instant GetCurrentTime()
        {
            return SystemClock.Instance.GetCurrentInstant();
        }
    }
}
