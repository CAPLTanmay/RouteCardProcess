using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Login;
using RouteCardProcess.Model.Entities;
using Microsoft.Extensions.Logging;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LogInController : ControllerBase
    {
        private readonly ILogInRepository _repo;
        private readonly IJwtTokenService _jwtService;
        private readonly ILogger<LogInController> _logger;
        private readonly ISystemLoggerRepository _systemLogger;
       

        public LogInController(ILogInRepository repo, IJwtTokenService jwtService, ILogger<LogInController> logger, ISystemLoggerRepository systemLogger)
        {
            _repo = repo;
            _jwtService = jwtService;
            _logger = logger;
            _systemLogger = systemLogger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var logins = await _repo.GetAllAsync();
                return Ok(logins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LogInMaster login)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _repo.AddAsync(login);
                return result > 0 ? Ok("User created") : StatusCode(500, "Insert failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, "Internal server error.");
            }
        }

        [AllowAnonymous]
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateLogin([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _repo.ValidateLoginAsync(request.OperatorId, request.Password);
                if (user == null)
                    return Unauthorized(new { message = "Invalid username or password" });

                var token = _jwtService.GenerateToken(request.OperatorId);
                return Ok(new { message = "Login successful", token, user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateLogin");
                await _systemLogger.LogAsync("LogInController", "ValidateLogin", ex.ToString());
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        [HttpPost("TryLogout")]
        public async Task<IActionResult> TryLogout([FromBody] LogoutRequest request)
        {
            try
            {
                var (flag, message) = await _repo.TryLogoutAsync(request.WorkCenterNo, request.WorkOrderNo, request.OperationNo);

                if (flag == 1)
                    return Ok(new { Success = true, Message = message });

                return BadRequest(new { Success = false, Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TryLogout");
                return StatusCode(500, new { Success = false, Message = "Internal server error." });
            }
        }
    }
}