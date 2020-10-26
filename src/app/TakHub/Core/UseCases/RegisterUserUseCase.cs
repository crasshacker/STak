using System.Linq;
using System.Threading.Tasks;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;
using STak.TakHub.Core.Interfaces.Gateways.Repositories;
using STak.TakHub.Core.Interfaces.UseCases;

namespace STak.TakHub.Core.UseCases
{
    public sealed class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IUserRepository m_userRepository;


        public RegisterUserUseCase(IUserRepository userRepository)
        {
            m_userRepository = userRepository;
        }


        public async Task<bool> Handle(RegisterUserRequest message, IOutputPort<RegisterUserResponse> outputPort)
        {
            var response = await m_userRepository.Create(message.FirstName, message.LastName, message.Email,
                                                                         message.UserName, message.Password);
            outputPort.Handle(response.Success ? new RegisterUserResponse(response.Id, true)
                                               : new RegisterUserResponse(response.Errors.Select(e => e.Description)));
            return response.Success;
        }
    }
}
