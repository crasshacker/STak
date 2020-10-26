using System;
using MessagePack;
using STak.TakEngine;
using STak.TakEngine.AI;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class PlayerDto
    {
        [Key(0)]       public int    Id       { get; set; }
        [Key(1)]       public string Name     { get; set; }
        [Key(2)]       public bool   IsAI     { get; set; }
        [Key(3)]       public bool   WasAI    { get; set; }
        [Key(4)]       public bool   IsRemote { get; set; }
        [Key(5)]       public bool   IsPaused { get; set; }
     // [IgnoreMember] public ITakAI AI       { get; set; }
    }
}
