using System;
using System.Collections.Generic;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class StackMoveDto
    {
        [Key(0)] public CellDto      StartingCell   { get; set; }
        [Key(1)] public DirectionDto Direction      { get; set; }
        [Key(2)] public int          StoneCount     { get; set; }
        [Key(3)] public int[]        DropCounts     { get; set; }
        [Key(4)] public int          LastDropCount  { get; set; }
        [Key(5)] public StackDto     GrabbedStack   { get; set; }
        [Key(7)] public StoneDto     FlattenedStone { get; set; }
        [Key(8)] public bool         HasExecuted    { get; set; }
    }
}
