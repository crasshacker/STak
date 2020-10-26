using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class StoneDto
    {
        [Key(0)] public int       Id       { get; set; }
        [Key(1)] public int       PlayerId { get; set; }
        [Key(2)] public StoneType Type     { get; set; }
    }
}
