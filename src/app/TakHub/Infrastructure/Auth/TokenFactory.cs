using System;
using System.Security.Cryptography;
using STak.TakHub.Core.Interfaces.Services;


namespace STak.TakHub.Infrastructure.Auth
{
    public sealed class TokenFactory : ITokenFactory
    {
        public string GenerateToken(int size=32)
        {
            var randomNumber = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
