using System.Threading.Tasks;
using STak.TakHub.Core.Dto;

namespace STak.TakHub.Core.Interfaces.Services
{
    public interface IJwtFactory
    {
        Task<AccessToken> GenerateEncodedToken(string id, string userName);
    }
}
