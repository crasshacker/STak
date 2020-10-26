using System;
using NodaTime;

namespace STak.TakHub.Interop
{
    public class JwtTokenInfo
    {
        public string  Token         { get; }
        public string  RefreshToken  { get; }
        public Instant ExpireInstant { get; }


        public JwtTokenInfo()
        {
            ExpireInstant = Instant.MinValue;
        }


        public JwtTokenInfo(string token, string refreshToken, Instant expireInstant)
        {
            Token         = token;
            RefreshToken  = refreshToken;
            ExpireInstant = expireInstant;
        }
    }
}
