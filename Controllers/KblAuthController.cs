using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Login;
using Microsoft.Extensions.Logging;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KblAuthController : ControllerBase
    {
        private readonly IKblAuthService _kblService;
        private readonly ILogger<KblAuthController> _logger;

        public KblAuthController(IKblAuthService kblService, ILogger<KblAuthController> logger)
        {
            _kblService = kblService;
            _logger = logger;
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
                    message = "Login successful",
                    token,
                    employee = empInfo.EmpInfo.FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
    }
}
