using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;

namespace STak.TakHub.Core.Dto.UseCaseRequests
{
    public class ExchangeRefreshTokenRequest : IUseCaseRequest<ExchangeRefreshTokenResponse>
    {
        public string AccessToken  { get; }
        public string RefreshToken { get; }
        public string SigningKey   { get; }


        public ExchangeRefreshTokenRequest(string accessToken, string refreshToken, string signingKey)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            SigningKey = signingKey;
        }
    }
}
