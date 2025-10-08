using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly ITokenBlacklistService _tokenBlacklistService;


        public LogInController(ILogInRepository repo, IJwtTokenService jwtService, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService, ITokenBlacklistService tokenBlacklistService)
        {
            _repo = repo;
            _jwtService = jwtService;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
            _tokenBlacklistService = tokenBlacklistService;
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
        [EnableRateLimiting("LoginRateLimit")]
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

                var token = await _jwtService.GenerateTokenAsync(request.OperatorId,user.OperatorRole);
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

        //[AllowAnonymous]
        //[EnableRateLimiting("LoginRateLimit")]
        //[HttpPost("loginEmployee")]
        //public async Task<IActionResult> LoginEmployee([FromBody] LoginRequest request)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //            return BadRequest(ModelState);

        //        var loginResult = await _repo.LoginEmployeeAsync(request.OperatorId, request.Password);

        //        if (!loginResult.IsSuccess)
        //        {
        //            return Unauthorized(new
        //            {
        //                message = loginResult.FailureReason ?? _userMessageService.GetMessage(1001)
        //            });
        //        }

        //        var token = await _jwtService.GenerateTokenAsync(request.OperatorId, loginResult.User.OperatorRole);
        //        var successMessage = _userMessageService.GetMessage(2001); // Login successful

        //        return Ok(new
        //        {
        //            message = successMessage,
        //            token,
        //            isTempPassword = loginResult.IsTempPassword,
        //            user = loginResult.User
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        await _systemLogger.LogAsync("LogInController", "LoginEmployee", ex.ToString());
        //        var errMsg = _userMessageService.GetMessage(5001); // Internal error
        //        return StatusCode(500, new { message = errMsg });
        //    }
        //}

        [AllowAnonymous]
        [EnableRateLimiting("LoginRateLimit")]
        [HttpPost("loginEmployee")]
        public async Task<IActionResult> LoginEmployee([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var loginResult = await _repo.LoginEmployeeAsync(request.OperatorId, request.Password);

                if (!loginResult.IsSuccess)
                    return Unauthorized(new { message = _userMessageService.GetMessage(1001) });

                var token = await _jwtService.GenerateTokenAsync(request.OperatorId, loginResult.User.OperatorRole);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,               // Only send over HTTPS
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(15),
                    Path = "/"
                };

                Response.Cookies.Append("AuthToken", token, cookieOptions);

                return Ok(new
                {
                    message = _userMessageService.GetMessage(2001),
                    isTempPassword = loginResult.IsTempPassword,
                    token,
                    user = loginResult.User
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInController", "LoginEmployee", ex.ToString());
                var errMsg = _userMessageService.GetMessage(5001);
                return StatusCode(500, new { message = errMsg });
            }
        }


        [Authorize]
        [HttpPost("TryLogout")]
        public async Task<IActionResult> TryLogout([FromBody] LogoutRequest request)
        {
            try
            {
                // STEP 1: Business Logic Validation
                var (flag, message) = await _repo.TryLogoutAsync(
                    request.WorkCenterNo,
                    request.WorkOrderNo,
                    request.OperationNo);

                if (flag == 0)
                {
                    // Not allowed to logout
                    return BadRequest(new { Success = false, Message = message });
                }

                // STEP 2: Extract current JWT details from the logged-in user
                var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var expUnix = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

                if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(expUnix))
                {
                    return Unauthorized(new { Success = false, Message = "Invalid or missing token claims" });
                }

                var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expUnix)).UtcDateTime;

                // STEP 3: Revoke token (add to blacklist table)
                await _tokenBlacklistService.RevokeTokenAsync(jti, exp);

                // STEP 4: Return unified success response
                return Ok(new
                {
                    Success = true,
                    Message = message, // this comes from your business logic (_userMessageService.GetMessage(1005))
                    TokenRevoked = true
                });
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