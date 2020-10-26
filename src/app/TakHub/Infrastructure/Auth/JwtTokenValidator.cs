using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using STak.TakHub.Core.Interfaces.Services;
using STak.TakHub.Infrastructure.Interfaces;

namespace STak.TakHub.Infrastructure.Auth
{
    public sealed class JwtTokenValidator : IJwtTokenValidator
    {
        private readonly IJwtTokenHandler m_jwtTokenHandler;


        public JwtTokenValidator(IJwtTokenHandler jwtTokenHandler)
        {
            m_jwtTokenHandler = jwtTokenHandler;
        }


        public ClaimsPrincipal GetPrincipalFromToken(string token, string signingKey)
        {
            return m_jwtTokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateAudience         = false,
                ValidateIssuer           = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateLifetime         = false // we check expired tokens here
            });
        }
    }
}
