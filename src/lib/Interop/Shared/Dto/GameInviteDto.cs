using System;
using System.Collections.Generic;
using MessagePack;
using NodaTime;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class GameInviteDto
    {
        [Key(0)]  public Guid           Id           { get; set; }
        [Key(1)]  public string         Inviter      { get; set; }
        [Key(2)]  public string         ConnectionId { get; set; }
        [Key(3)]  public int[]          PlayerNumber { get; set; }
        [Key(4)]  public int[]          BoardSize    { get; set; }
        [Key(5)]  public string[]       Opponent     { get; set; }
        [Key(6)]  public bool           IsInviterAI  { get; set; }
        [Key(7)]  public bool           WillPlayAI   { get; set; }
        [Key(8)]  public HubGameTypeDto GameType     { get; set; }
        [Key(9)]  public long           CreateTime   { get; set; }
        // The System.Text.Json serializer doesn't support ValueTuples,
        // so we extract the individual values from these two tuples.
        [Key(10)] public int            TimeLimitMin { get; set; }
        [Key(11)] public int            TimeLimitMax { get; set; }
        [Key(12)] public int            IncrementMin { get; set; }
        [Key(13)] public int            IncrementMax { get; set; }
    }
}
