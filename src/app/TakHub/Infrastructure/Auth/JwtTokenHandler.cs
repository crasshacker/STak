using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using STak.TakHub.Core.Interfaces.Services;
using STak.TakHub.Infrastructure.Interfaces;

namespace STak.TakHub.Infrastructure.Auth
{
    public sealed class JwtTokenHandler : IJwtTokenHandler
    {
        private readonly JwtSecurityTokenHandler m_jwtSecurityTokenHandler;
        private readonly ILogger                 m_logger;


        public JwtTokenHandler(ILogger logger)
        {
            if (m_jwtSecurityTokenHandler == null)
                m_jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

            m_logger = logger;
        }


        public string WriteToken(JwtSecurityToken jwt)
        {
            return m_jwtSecurityTokenHandler.WriteToken(jwt);
        }


        public ClaimsPrincipal ValidateToken(string token, TokenValidationParameters tokenValidationParameters)
        {
            try
            {
                var principal = m_jwtSecurityTokenHandler.ValidateToken(token, tokenValidationParameters,
                                                                                  out var securityToken);

                if (! (securityToken is JwtSecurityToken jwtSecurityToken) || ! jwtSecurityToken.Header.Alg.Equals(
                                        SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch (Exception e)
            {
                m_logger.LogError($"Token validation failed: {e.Message}");
                return null;
            }
        }
    }
}
