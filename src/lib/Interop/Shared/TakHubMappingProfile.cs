using System;
using System.Linq;
using System.Collections.Generic;
using NodaTime;
using AutoMapper;
using STak.TakEngine;
using STak.TakHub.Interop.Dto;

namespace STak.TakHub.Interop
{
    public class TakHubMappingProfile : Profile
    {
        public TakHubMappingProfile()
        {
            CreateMap<BoardPosition, BoardPositionDto>();
            CreateMap<BoardPositionDto, BoardPosition>();

            CreateMap<Cell, CellDto>();
            CreateMap<CellDto, Cell>();

            CreateMap<Direction, DirectionDto>();
            CreateMap<DirectionDto, Direction>();

            CreateMap<GameInvite, GameInviteDto>()
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime.ToUnixTimeTicks()))
                .ForMember(dest => dest.TimeLimitMin, opt => opt.MapFrom(src => src.TimeLimit.Min))
                .ForMember(dest => dest.TimeLimitMax, opt => opt.MapFrom(src => src.TimeLimit.Max))
                .ForMember(dest => dest.IncrementMin, opt => opt.MapFrom(src => src.Increment.Min))
                .ForMember(dest => dest.IncrementMax, opt => opt.MapFrom(src => src.Increment.Max));
            CreateMap<GameInviteDto, GameInvite>()
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => Instant.FromUnixTimeTicks(src.CreateTime)))
                .ForMember(dest => dest.TimeLimit, opt => opt.MapFrom(src =>
                            new Tuple<int, int>(src.TimeLimitMin, src.TimeLimitMax).ToValueTuple()))
                .ForMember(dest => dest.Increment, opt => opt.MapFrom(src =>
                            new Tuple<int, int>(src.IncrementMin, src.IncrementMax).ToValueTuple()));

            CreateMap<GamePrototype, GamePrototypeDto>();
            CreateMap<GamePrototypeDto, GamePrototype>();

            CreateMap<GameTimer, GameTimerDto>()
                .ConvertUsing(new GameTimerTypeConverter());

            CreateMap<GameTimerDto, GameTimer>()
                .ConvertUsing(new GameTimerDtoTypeConverter());

            CreateMap<HubGameType, HubGameTypeDto>();
            CreateMap<HubGameTypeDto, HubGameType>();

            CreateMap<IMove, MoveDto>().ConstructUsing(move => new MoveDto(move))
                .ForMember(dest => dest.StoneMoveDto, opt => opt.MapFrom(src => src as StoneMove))
                .ForMember(dest => dest.StackMoveDto, opt => opt.MapFrom(src => src as StackMove));
            CreateMap<MoveDto, IMove>().ConstructUsing(moveDto => MoveDto.Convert(moveDto));

            CreateMap<StoneMove, StoneMoveDto>();
            CreateMap<StoneMoveDto, StoneMove>();

            CreateMap<StackMove, StackMoveDto>();
            CreateMap<StackMoveDto, StackMove>();

            CreateMap<Player, PlayerDto>();
            CreateMap<PlayerDto, Player>()
                .ForMember(dest => dest.AI, opt => opt.Ignore());

            CreateMap<Stack, StackDto>();
            CreateMap<StackDto, Stack>();

            CreateMap<Stone, StoneDto>();
            CreateMap<StoneDto, Stone>();
        }


        private class GameTimerTypeConverter : ITypeConverter<GameTimer, GameTimerDto>
        {
            public GameTimerDto Convert(GameTimer source, GameTimerDto destination, ResolutionContext context)
            {
                return source.GameLimit == GameTimer.Unlimited.GameLimit ? GameTimerDto.Unlimited
                    : new GameTimerDto
                    {
                        StartOnMove = source.StartTimerOnFirstMove,
                        GameLimit   = source.GameLimit.TotalMilliseconds,
                        Increment   = source.Increment.TotalMilliseconds,
                        TimeLimits  = new double[] { source.TimeLimits[Player.One].TotalMilliseconds,
                                                     source.TimeLimits[Player.Two].TotalMilliseconds },
                        Remaining   = new double[] { source.GetRemainingTime(Player.One).TotalMilliseconds,
                                                     source.GetRemainingTime(Player.Two).TotalMilliseconds }
                    };
            }
        }


        private class GameTimerDtoTypeConverter : ITypeConverter<GameTimerDto, GameTimer>
        {
            public GameTimer Convert(GameTimerDto source, GameTimer destination, ResolutionContext context)
            {
                // FIXIT - This doesn't set the TimeLimits properly (doesn't account for added increments).
                return source.GameLimit == GameTimerDto.Unlimited.GameLimit ? GameTimer.Unlimited
                    : new GameTimer(TimeSpan.FromMilliseconds(source.GameLimit),
                                    TimeSpan.FromMilliseconds(source.Increment),
                                    source.StartOnMove,
                                    new TimeSpan[] { TimeSpan.FromMilliseconds(source.Remaining[Player.One]),
                                                     TimeSpan.FromMilliseconds(source.Remaining[Player.Two]) });
            }
        }
    }
}
