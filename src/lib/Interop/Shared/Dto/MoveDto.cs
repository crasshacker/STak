using System;
using MessagePack;
using STak.TakEngine;

namespace STak.TakHub.Interop.Dto
{
    [Serializable]
    [MessagePackObject]
    public class MoveDto
    {
        [Key(0)] public StoneMoveDto StoneMoveDto { get; set; }
        [Key(1)] public StackMoveDto StackMoveDto { get; set; }


        public MoveDto()
        {
        }


        public MoveDto(IMove move)
        {
            StoneMoveDto = (move is StoneMove stoneMove) ? Mapper.Map<StoneMoveDto>(stoneMove) : null;
            StackMoveDto = (move is StackMove stackMove) ? Mapper.Map<StackMoveDto>(stackMove) : null;
        }


        public static IMove Convert(MoveDto dto)
        {
            return (dto.StoneMoveDto != null) ? (IMove) Mapper.Map<StoneMove>(dto.StoneMoveDto)
                 : (dto.StackMoveDto != null) ? (IMove) Mapper.Map<StackMove>(dto.StackMoveDto)
                 : null;
        }
    }
}
