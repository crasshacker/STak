using System;
using System.Threading.Tasks;
using STak.TakHub.Core.Dto;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;
using STak.TakHub.Core.Interfaces.Gateways.Repositories;
using STak.TakHub.Core.Interfaces.Services;
using STak.TakHub.Core.Interfaces.UseCases;

namespace STak.TakHub.Core.UseCases
{
    public sealed class LoginUseCase : ILoginUseCase
    {
        private readonly IUserRepository       m_userRepository;
        private readonly IJwtFactory           m_jwtFactory;
        private readonly ITokenFactory         m_tokenFactory;


        public LoginUseCase(IUserRepository userRepository, IJwtFactory jwtFactory, ITokenFactory tokenFactory)
        {
            m_userRepository = userRepository;
            m_jwtFactory     = jwtFactory;
            m_tokenFactory   = tokenFactory;
        }


        public async Task<bool> Handle(LoginRequest message, IOutputPort<LoginResponse> outputPort)
        {
            string userName = message.UserName;

            if (! String.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(message.Password))
            {
                // ensure we have a user with the given user name
                var user = await m_userRepository.FindByName(userName);
                if (user != null)
                {
                    // validate password
                    if (await m_userRepository.CheckPassword(user, message.Password))
                    {
                        // generate refresh token
                        var refreshToken = m_tokenFactory.GenerateToken();
                        user.AddRefreshToken(refreshToken, user.Id, message.RemoteIpAddress);
                        await m_userRepository.Update(user);

                        // generate access token
                        outputPort.Handle(new LoginResponse(await m_jwtFactory.GenerateEncodedToken(user.IdentityId,
                                                                                      userName), refreshToken, true));
                        return true;
                    }
                }
            }
            outputPort.Handle(new LoginResponse(new[] { new Error("login_failure", "Invalid username or password.") }));
            return false;
        }
    }
}
