using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LogInController : ControllerBase
    {
        private readonly ILogInRepository _repo;
        private readonly IJwtTokenService _jwtService;

        public LogInController(ILogInRepository repo, IJwtTokenService jwtService)
        {
            _repo = repo;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var logins = await _repo.GetAllAsync();
            return Ok(logins);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LogInMaster login)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _repo.AddAsync(login);
            return result > 0 ? Ok("User created") : StatusCode(500, "Insert failed");
        }

        [AllowAnonymous]
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateLogin([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _repo.ValidateLoginAsync(request.OperatorId, request.Password);
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password" });

            var token = _jwtService.GenerateToken(request.OperatorId);
            return Ok(new { message = "Login successful", token, user });
        }

        [HttpPost("TryLogout")]
        public async Task<IActionResult> TryLogout([FromBody] LogoutRequest request)
        {
            var (flag, message) = await _repo.TryLogoutAsync(request.WorkCenterNo, request.WorkOrderNo, request.OperationNo);

            if (flag == 1)
                return Ok(new { Success = true, Message = message });

            return BadRequest(new { Success = false, Message = message });
        }
    }
}
