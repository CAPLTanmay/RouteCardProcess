using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KblAuthController : ControllerBase
    {
        private readonly IKblAuthService _kblService;

        public KblAuthController(IKblAuthService kblService)
        {
            _kblService = kblService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> FullLogin([FromBody] KblLoginRequest request)
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
    }
}
