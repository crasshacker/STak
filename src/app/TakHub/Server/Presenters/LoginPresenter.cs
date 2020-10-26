using System.Net;
using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;
using STak.TakHub.Serialization;

namespace STak.TakHub.Presenters
{
    public sealed class LoginPresenter : IOutputPort<LoginResponse>
    {
        public JsonContentResult ContentResult { get; }


        public LoginPresenter()
        {
            ContentResult = new JsonContentResult();
        }


        public void Handle(LoginResponse response)
        {
            ContentResult.StatusCode = (int)(response.Success ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
            ContentResult.Content = response.Success
                ? JsonSerializer.SerializeObject(new LoginResponse(response.AccessToken, response.RefreshToken))
                : JsonSerializer.SerializeObject(response.Errors);
        }
    }
}
