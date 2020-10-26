using System.Threading.Tasks;
using STak.TakHub.Core.Domain.Entities;
using STak.TakHub.Core.Dto.GatewayResponses.Repositories;

namespace STak.TakHub.Core.Interfaces.Gateways.Repositories
{
    public interface IUserRepository  : IRepository<User>
    {
        Task<CreateUserResponse> Create(string firstName, string lastName, string email, string userName, string password);
        Task<User> FindByName(string userName);
        Task<bool> CheckPassword(User user, string password);
    }
}
