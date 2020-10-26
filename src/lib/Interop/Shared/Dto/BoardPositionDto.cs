using System;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class BoardPositionDto
    {
        [Key(0)] public double File { get; set; }
        [Key(1)] public double Rank { get; set; }
    }
}
