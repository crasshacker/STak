using System.Net;
using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;
using STak.TakHub.Serialization;

namespace STak.TakHub.Presenters
{
    public sealed class ExchangeRefreshTokenPresenter : IOutputPort<ExchangeRefreshTokenResponse>
    {
        public JsonContentResult ContentResult { get; }


        public ExchangeRefreshTokenPresenter()
        {
            ContentResult = new JsonContentResult();
        }


        public void Handle(ExchangeRefreshTokenResponse response)
        {
            ContentResult.StatusCode = (int)(response.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            ContentResult.Content = response.Success ? JsonSerializer.SerializeObject(new ExchangeRefreshTokenResponse(response.AccessToken, response.RefreshToken)) : JsonSerializer.SerializeObject(response.Message);
        }
    }
}
