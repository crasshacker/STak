using System.Net;
using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;
using STak.TakHub.Serialization;

namespace STak.TakHub.Presenters
{
    public sealed class LogoutPresenter : IOutputPort<LogoutResponse>
    {
        public JsonContentResult ContentResult { get; }


        public LogoutPresenter()
        {
            ContentResult = new JsonContentResult();
        }


        public void Handle(LogoutResponse response)
        {
            ContentResult.StatusCode = (int)(response.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            ContentResult.Content = response.Success ? JsonSerializer.SerializeObject(new LogoutResponse())
                : JsonSerializer.SerializeObject(response.Errors);
        }
    }
}
