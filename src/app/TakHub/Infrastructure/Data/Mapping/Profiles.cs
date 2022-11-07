using AutoMapper;
using STak.TakHub.Core.Domain.Entities;
using STak.TakHub.Infrastructure.Identity;

namespace STak.TakHub.Infrastructure.Data.Mapping
{
    public class DataProfile : Profile
    {
        public DataProfile()
        {
            CreateMap<User, AppUser>().ConstructUsing(u => new AppUser {UserName = u.UserName, Email = u.Email}).ForMember(au=>au.Id, opt=>opt.Ignore());
            CreateMap<AppUser, User>().ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email)).
                                       ForMember(dest=> dest.PasswordHash, opt=> opt.MapFrom(src=>src.PasswordHash)).
                                       ForMember(dest=> dest.Id, opt=> opt.Ignore());
                                    // ForAllOtherMembers(opt=>opt.Ignore());
        }
    }
}
