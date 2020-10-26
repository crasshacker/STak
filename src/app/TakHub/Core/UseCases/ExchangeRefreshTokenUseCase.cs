using System.Linq;
using System.Threading.Tasks;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;
using STak.TakHub.Core.Interfaces.Gateways.Repositories;
using STak.TakHub.Core.Interfaces.Services;
using STak.TakHub.Core.Interfaces.UseCases;
using STak.TakHub.Core.Specifications;


namespace STak.TakHub.Core.UseCases
{
    public sealed class ExchangeRefreshTokenUseCase : IExchangeRefreshTokenUseCase
    {
        private readonly IJwtTokenValidator m_jwtTokenValidator;
        private readonly IUserRepository    m_userRepository;
        private readonly IJwtFactory        m_jwtFactory;
        private readonly ITokenFactory      m_tokenFactory;


        public ExchangeRefreshTokenUseCase(IJwtTokenValidator jwtTokenValidator, IUserRepository userRepository, IJwtFactory jwtFactory, ITokenFactory tokenFactory)
        {
            m_jwtTokenValidator = jwtTokenValidator;
            m_userRepository    = userRepository;
            m_jwtFactory        = jwtFactory;
            m_tokenFactory      = tokenFactory;
        }

        public async Task<bool> Handle(ExchangeRefreshTokenRequest message, IOutputPort<ExchangeRefreshTokenResponse> outputPort)
        {
            var cp = m_jwtTokenValidator.GetPrincipalFromToken(message.AccessToken, message.SigningKey);

            // invalid token/signing key was passed and we can't extract user claims
            if (cp != null)
            {
                var id = cp.Claims.First(c => c.Type == "id");
                var user = await m_userRepository.GetSingleBySpec(new UserSpecification(id.Value));

                if (user.HasValidRefreshToken(message.RefreshToken))
                {
                    var jwtToken = await m_jwtFactory.GenerateEncodedToken(user.IdentityId, user.UserName);
                    var refreshToken = m_tokenFactory.GenerateToken();
                    user.RemoveRefreshToken(message.RefreshToken); // delete the token we've exchanged
                    user.AddRefreshToken(refreshToken, user.Id, ""); // add the new one
                    await m_userRepository.Update(user);
                    outputPort.Handle(new ExchangeRefreshTokenResponse(jwtToken, refreshToken, true));
                    return true;
                }
            }
            outputPort.Handle(new ExchangeRefreshTokenResponse(false, "Invalid token."));
            return false;
        }
    }
}
