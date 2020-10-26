using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;

namespace STak.TakHub.Core.Dto.UseCaseRequests
{
    public class LogoutRequest : IUseCaseRequest<LogoutResponse>
    {
        public string UserName { get; }


        public LogoutRequest(string userName)
        {
            UserName = userName;
        }
    }
}
