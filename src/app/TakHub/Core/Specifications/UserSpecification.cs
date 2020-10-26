using STak.TakHub.Core.Domain.Entities;

namespace STak.TakHub.Core.Specifications
{
    public sealed class UserSpecification : BaseSpecification<User>
    {
        public UserSpecification(string identityId) : base(u => u.IdentityId==identityId)
        {
            AddInclude(u => u.RefreshTokens);
        }
    }
}
