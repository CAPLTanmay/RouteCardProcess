using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Login;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KblAuthController : ControllerBase
    {
        private readonly IKblAuthService _kblService;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;

        public KblAuthController(IKblAuthService kblService, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _kblService = kblService;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> FullLogin([FromBody] KblLoginRequest request)
        {
            try
            {
                var loginResult = await _kblService.AuthenticateLoginAsync(request);

                if (loginResult != "Success")
                    return Unauthorized(new { message = loginResult });

                var token = await _kblService.GetTokenAsync();
                var empInfo = await _kblService.GetEmployeeInfoAsync(token, request.StrLoginId);

                return Ok(new
                {
                    message = _userMessageService.GetMessage(2001), // Login successful
                    token,
                    employee = empInfo.EmpInfo.FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("KblAuthController", "FullLogin", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001) });
            }
        }
    }
}
