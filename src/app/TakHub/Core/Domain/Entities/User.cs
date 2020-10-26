using System;
using System.Collections.Generic;
using System.Linq;
using STak.TakHub.Core.Shared;

namespace STak.TakHub.Core.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FirstName    { get; private set; } // EF migrations require at least a private setter.
        public string LastName     { get; private set; }
        public string IdentityId   { get; private set; }
        public string UserName     { get; private set; } // Required by automapper.
        public string Email        { get; private set; }
        public string PasswordHash { get; private set; }

        private readonly List<RefreshToken> m_refreshTokens = new List<RefreshToken>();
        public IReadOnlyCollection<RefreshToken> RefreshTokens => m_refreshTokens.AsReadOnly();

        internal User() { /* Required by EF */ }


        internal User(string firstName, string lastName, string identityId, string userName)
        {
            FirstName  = firstName;
            LastName   = lastName;
            IdentityId = identityId;
            UserName   = userName;
        }


        public bool HasValidRefreshToken(string refreshToken)
        {
            return m_refreshTokens.Any(rt => rt.Token == refreshToken && rt.Active);
        }


        public void AddRefreshToken(string token, int userId, string remoteIpAddress, double daysToExpire = 5)
        {
            m_refreshTokens.Add(new RefreshToken(token, DateTime.UtcNow.AddDays(daysToExpire), userId, remoteIpAddress));
        }


        public void RemoveRefreshToken(string refreshToken)
        {
            m_refreshTokens.Remove(m_refreshTokens.First(t => t.Token == refreshToken));
        }
    }
}
