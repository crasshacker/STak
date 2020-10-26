using System;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class StoneMoveDto
    {
        [Key(0)] public CellDto  TargetCell  { get; set; }
        [Key(1)] public StoneDto Stone       { get; set; }
        [Key(2)] public bool     HasExecuted { get; set; }
    }
}
