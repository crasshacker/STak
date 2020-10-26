using System;

namespace STak.TakHub.Client
{
    public class HubGameOptions
    {
        public int  TrackingRate { get; set; } = 10;    // notifications per second
        public bool UseLocalAI   { get; set; } = false; // local AI vs. remote AI (for testing purposes)
    }
}
