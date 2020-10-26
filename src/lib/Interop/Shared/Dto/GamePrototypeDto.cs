using System;
using System.Linq;
using System.Collections.Generic;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class GamePrototypeDto
    {
        [Key(0)] public Guid         Id        { get; set; }
        [Key(1)] public int          BoardSize { get; set; }
        [Key(2)] public PlayerDto    PlayerOne { get; set; }
        [Key(3)] public PlayerDto    PlayerTwo { get; set; }
        [Key(4)] public GameTimerDto GameTimer { get; set; }
        [Key(5)] public MoveDto[]    Moves     { get; set; }
    }
}
