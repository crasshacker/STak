using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Interfaces.UseCases;
using STak.TakHub.Models.Settings;
using STak.TakHub.Presenters;

namespace STak.TakHub.Controllers
{
    [ApiController]
    // [Authorize(Policy = "ApiUser")]
    [Route("takhub/api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILoginUseCase                 m_loginUseCase;
        private readonly LoginPresenter                m_loginPresenter;
        private readonly ILogoutUseCase                m_logoutUseCase;
        private readonly LogoutPresenter               m_logoutPresenter;
        private readonly IExchangeRefreshTokenUseCase  m_exchangeRefreshTokenUseCase;
        private readonly ExchangeRefreshTokenPresenter m_exchangeRefreshTokenPresenter;
        private readonly AuthSettings                  m_authSettings;
        

        public AuthController(ILoginUseCase                 loginUseCase,
                              LoginPresenter                loginPresenter,
                              ILogoutUseCase                logoutUseCase,
                              LogoutPresenter               logoutPresenter,
                              IExchangeRefreshTokenUseCase  exchangeRefreshTokenUseCase,
                              ExchangeRefreshTokenPresenter exchangeRefreshTokenPresenter,
                              IOptions<AuthSettings>        authSettings)
        {
            m_loginUseCase                  = loginUseCase;
            m_loginPresenter                = loginPresenter;
            m_logoutUseCase                 = logoutUseCase;
            m_logoutPresenter               = logoutPresenter;
            m_exchangeRefreshTokenUseCase   = exchangeRefreshTokenUseCase;
            m_exchangeRefreshTokenPresenter = exchangeRefreshTokenPresenter;
            m_authSettings                  = authSettings.Value;
        }


        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] Models.Request.LoginRequest request)
        {
            if (! ModelState.IsValid) { return BadRequest(ModelState); }

            await m_loginUseCase.Handle(new LoginRequest(request.UserName, request.Password,
                Request.HttpContext.Connection.RemoteIpAddress?.ToString()), m_loginPresenter);
            return m_loginPresenter.ContentResult;
        }


        [HttpPost("refreshtoken")]
        public async Task<ActionResult> RefreshToken([FromBody] Models.Request.ExchangeRefreshTokenRequest request)             {
            if (! ModelState.IsValid) { return BadRequest(ModelState);}

            await m_exchangeRefreshTokenUseCase.Handle(new ExchangeRefreshTokenRequest(request.AccessToken,
                         request.RefreshToken, m_authSettings.SecretKey), m_exchangeRefreshTokenPresenter);
            return m_exchangeRefreshTokenPresenter.ContentResult;
        }


        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] Models.Request.LogoutRequest request)
        {
            if (! ModelState.IsValid) { return BadRequest(ModelState);}

            await m_logoutUseCase.Handle(new LogoutRequest(request.UserName), m_logoutPresenter);
            return m_logoutPresenter.ContentResult;
        }
    }
}
