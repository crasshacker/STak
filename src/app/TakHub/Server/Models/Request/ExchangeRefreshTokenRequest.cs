using System;

namespace STak.TakHub.Models.Request
{
    public class ExchangeRefreshTokenRequest
    {
        public string AccessToken  { get; set; }
        public string RefreshToken { get; set; }
    }
}

