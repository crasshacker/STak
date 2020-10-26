using System;
using System.Threading.Tasks;
using STak.TakHub.Core.Dto;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces.UseCases;
using STak.TakHub.Core.Interfaces;

namespace STak.TakHub.Core.UseCases
{
    public sealed class LogoutUseCase : ILogoutUseCase
    {
        public LogoutUseCase()
        {
        }


        public Task<bool> Handle(LogoutRequest message, IOutputPort<LogoutResponse> outputPort)
        {
            string userName = message.UserName;

            if (String.IsNullOrEmpty(userName))
            {
                outputPort.Handle(new LogoutResponse(new[] { new Error("logout_failure", "Empty username.") }));
            }
            else
            {
                outputPort.Handle(new LogoutResponse());
            }
            return Task<bool>.FromResult(true);
        }
    }
}
