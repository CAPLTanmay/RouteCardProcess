using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Login;
using RouteCardProcess.Model.Entities;
using RouteCardProcess.Repositories;


namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LogInController : ControllerBase
    {
        private readonly ILogInRepository _repo;
        private readonly IJwtTokenService _jwtService;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;

        public LogInController(ILogInRepository repo, IJwtTokenService jwtService, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _repo = repo;
            _jwtService = jwtService;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
        }

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var logins = await _repo.GetAllAsync();
                return Ok(logins);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInController", "GetAllLogin", ex.ToString());
                var message = _userMessageService.GetMessage(5001);
                return StatusCode(500, message);
            }
        }

        [HttpPost("AddUser")]
        public async Task<IActionResult> Create([FromBody] LogInMaster login)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _repo.AddAsync(login);
                var message = result > 0
           ? _userMessageService.GetMessage(1002)
           : _userMessageService.GetMessage(1003);

                return result > 0
                    ? Ok(message)
                    : StatusCode(500, message);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInController", "CreateLogin", ex.ToString());
                var message = _userMessageService.GetMessage(5001);
                return StatusCode(500, message);
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
                {
                    var message = _userMessageService.GetMessage(1001); // Invalid login
                    return Unauthorized(new { message });
                }

                var token = await _jwtService.GenerateTokenAsync(request.OperatorId); 
                var successMessage = _userMessageService.GetMessage(2001); // Login successful

                return Ok(new { message = successMessage, token, user });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInController", "ValidateLogin", ex.ToString());
                var errMsg = _userMessageService.GetMessage(5001); // Internal error
                return StatusCode(500, new { message = errMsg });
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
                await _systemLogger.LogAsync("LogInController", "TryLogout", ex.ToString());
                var errorMessage = _userMessageService.GetMessage(5001);
                return StatusCode(500, new { Success = false, Message = errorMessage });
            }
        }
    }
}