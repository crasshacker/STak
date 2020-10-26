using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Interfaces.UseCases;
using STak.TakHub.Presenters;

namespace STak.TakHub.Controllers
{
    // [ApiController]
    // [Authorize(Policy = "ApiUser")]
    [Route("takhub/api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IRegisterUserUseCase m_registerUserUseCase;
        private readonly RegisterUserPresenter m_registerUserPresenter;


        public AccountsController(IRegisterUserUseCase registerUserUseCase, RegisterUserPresenter registerUserPresenter)
        {
            m_registerUserUseCase   = registerUserUseCase;
            m_registerUserPresenter = registerUserPresenter;
        }


        // POST api/accounts
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] Models.Request.RegisterUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await m_registerUserUseCase.Handle(new RegisterUserRequest(request.FirstName, request.LastName, request.Email, request.UserName, request.Password), m_registerUserPresenter);
            return m_registerUserPresenter.ContentResult;
        }
    }
}
