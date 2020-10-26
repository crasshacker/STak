using System;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class CellDto
    {
        [Key(0)] public int File { get; set; }
        [Key(1)] public int Rank { get; set; }
    }
}
