using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using STak.TakHub.Core.Domain.Entities;
using STak.TakHub.Core.Dto;
using STak.TakHub.Core.Dto.GatewayResponses.Repositories;
using STak.TakHub.Core.Interfaces.Gateways.Repositories;
using STak.TakHub.Core.Specifications;
using STak.TakHub.Infrastructure.Identity;

namespace STak.TakHub.Infrastructure.Data.Repositories
{
    internal sealed class UserRepository : EfRepository<User>, IUserRepository
    {
        private readonly UserManager<AppUser> m_userManager;
        private readonly IMapper              m_mapper;
        

        public UserRepository(UserManager<AppUser> userManager, IMapper mapper, AppDbContext appDbContext)
            : base(appDbContext)
        {
            m_userManager = userManager;
            m_mapper      = mapper;
        }


        public async Task<CreateUserResponse> Create(string firstName, string lastName, string email, string userName,
                                                                                                      string password)
        {
            var appUser = new AppUser {Email = email, UserName = userName};
            var identityResult = await m_userManager.CreateAsync(appUser, password);

            if (! identityResult.Succeeded)
            {
                return new CreateUserResponse(appUser.Id, false, identityResult.Errors.Select(
                                                       e => new Error(e.Code, e.Description)));
            }
          
            var user = new User(firstName, lastName, appUser.Id, appUser.UserName);
            m_appDbContext.Users.Add(user);
            await m_appDbContext.SaveChangesAsync();

            return new CreateUserResponse(appUser.Id, identityResult.Succeeded, identityResult.Succeeded
                           ? null : identityResult.Errors.Select(e => new Error(e.Code, e.Description)));
        }


        public async Task<User> FindByName(string userName)
        {
            var appUser = await m_userManager.FindByNameAsync(userName);
            return appUser == null ? null : m_mapper.Map(appUser, await GetSingleBySpec(new UserSpecification(
                                                                                                 appUser.Id)));
        }


        public async Task<bool> CheckPassword(User user, string password)
        {
            return await m_userManager.CheckPasswordAsync(m_mapper.Map<AppUser>(user), password);
        }
    }
}
