using System;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class DirectionDto
    {
        [Key(0)] public int Value { get; set; }
    }
}
