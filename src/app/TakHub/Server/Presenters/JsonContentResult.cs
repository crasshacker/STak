using Microsoft.AspNetCore.Mvc;

namespace STak.TakHub.Presenters
{
    public sealed class JsonContentResult : ContentResult
    {
        public JsonContentResult()
        {
            ContentType = "application/json";
        }
    }
}
