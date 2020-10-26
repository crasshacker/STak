using System;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class GameTimerDto
    {
        public static readonly GameTimerDto Unlimited = new GameTimerDto();

        [Key(0)] public double   GameLimit   { get; set; }
        [Key(1)] public double   Increment   { get; set; }
        [Key(2)] public double[] Remaining   { get; set; }
        [Key(3)] public double[] TimeLimits  { get; set; }
        [Key(4)] public bool     StartOnMove { get; set; }
    }
}
