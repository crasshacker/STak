using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using STak.TakHub.Core.Dto;
using STak.TakHub.Core.Interfaces.Services;
using STak.TakHub.Infrastructure.Interfaces;

namespace STak.TakHub.Infrastructure.Auth
{
    public sealed class JwtFactory : IJwtFactory
    {
        private readonly IJwtTokenHandler m_jwtTokenHandler;
        private readonly JwtIssuerOptions m_jwtOptions;


        public JwtFactory(IJwtTokenHandler jwtTokenHandler, IOptions<JwtIssuerOptions> jwtOptions)
        {
            m_jwtTokenHandler = jwtTokenHandler;
            m_jwtOptions      = jwtOptions.Value;
            ThrowIfInvalidOptions(m_jwtOptions);
        }


        /// <returns>Date converted to seconds since Unix epoch (Jan 1, 1970, midnight UTC).</returns>
        private static long ToUnixEpochDate(DateTime date)
          => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(
                         1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);


        public async Task<AccessToken> GenerateEncodedToken(string id, string userName)
        {
            var identity = GenerateClaimsIdentity(id, userName);

            var claims = new[]
            {
                 new Claim(JwtRegisteredClaimNames.Sub, userName),
                 new Claim(JwtRegisteredClaimNames.Jti, await m_jwtOptions.JtiGenerator()),
                 new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(m_jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
                 identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol),
                 identity.FindFirst(Helpers.Constants.Strings.JwtClaimIdentifiers.Id)
             };

            // Create the JWT security token and encode it.
            var jwt = new JwtSecurityToken(
                m_jwtOptions.Issuer,
                m_jwtOptions.Audience,
                claims,
                m_jwtOptions.NotBefore,
                m_jwtOptions.Expiration,
                m_jwtOptions.SigningCredentials);
          
            return new AccessToken(m_jwtTokenHandler.WriteToken(jwt), (int)m_jwtOptions.ValidFor.TotalSeconds);
        }


        private static ClaimsIdentity GenerateClaimsIdentity(string id, string userName)
        {
            return new ClaimsIdentity(new GenericIdentity(userName, "Token"), new[]
            {
                new Claim(Helpers.Constants.Strings.JwtClaimIdentifiers.Id, id),
                new Claim(Helpers.Constants.Strings.JwtClaimIdentifiers.Rol, Helpers.Constants.Strings.JwtClaims.ApiAccess)
            });
        }


        private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.ValidFor <= TimeSpan.Zero)
            {
                throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(JwtIssuerOptions.ValidFor));
            }

            if (options.SigningCredentials == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
            }

            if (options.JtiGenerator == null)
            {
                throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
            }
        }
    }
}
