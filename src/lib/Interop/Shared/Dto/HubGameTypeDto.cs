using System;
using MessagePack;

namespace STak.TakHub.Interop
{
    [Serializable]
    [MessagePackObject]
    public class HubGameTypeDto
    {
        [Key(0)] public int Mask { get; set; }
    }
}
