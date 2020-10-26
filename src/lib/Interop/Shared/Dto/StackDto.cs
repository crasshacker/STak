using System;
using System.Linq;
using System.Collections.Generic;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class StackDto
    {
        [Key(0)] public CellDto    Cell   { get; set; }
        [Key(1)] public StoneDto[] Stones { get; set; }
    }
}
