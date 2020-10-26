using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Dto.UseCaseResponses;

namespace STak.TakHub.Core.Interfaces.UseCases
{
    public interface ILogoutUseCase : IUseCaseRequestHandler<LogoutRequest, LogoutResponse>
    {
    }
}
