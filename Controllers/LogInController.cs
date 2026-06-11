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
    [Route("api/validate")]
    [Authorize]
    public class LogInController : ControllerBase
    {
        private readonly ILogInRepository _repo;
        private readonly IJwtTokenService _jwtService;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly LoginAttemptService _loginAttemptService;
        private readonly IConfiguration _configuration;

        public LogInController(ILogInRepository repo, IJwtTokenService jwtService, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService, ITokenBlacklistService tokenBlacklistService, LoginAttemptService loginAttemptService, IConfiguration configuration)
        {
            _repo = repo;
            _jwtService = jwtService;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
            _tokenBlacklistService = tokenBlacklistService;
            _loginAttemptService = loginAttemptService;
            _configuration = configuration;
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

                return Ok(new { message = successMessage,
                    token,
                    user });

            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LogInController", "ValidateLogin", ex.ToString());
                var errMsg = _userMessageService.GetMessage(5001); // Internal error
                return StatusCode(500, new { message = errMsg });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("LoginRateLimit")]
        [HttpPost("validateEmployee")]
        public async Task<IActionResult> LoginEmployee([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // 1. Create lockout key (Per Operator)
                string key = request.OperatorId;

                // 2. Check lockout
                var lockStatus = _loginAttemptService.CheckAttempt(key);
                if (lockStatus.IsLocked)
                {
                    int minutes = lockStatus.RemainingLockout?.Minutes ?? 0;

                    return Unauthorized(new
                    {
                        message = $"User locked due to multiple failed attempts. Try after {minutes} minutes"
                    });
                }

                // 3. Validate login
                var loginResult = await _repo.LoginEmployeeAsync(request.OperatorId, request.Password);

                if (!loginResult.IsSuccess)
                {
                    // Register failed attempt
                    _loginAttemptService.RegisterFailedAttempt(key);

                    // Wrong password message
                    return Unauthorized(new
                    {
                        message = _userMessageService.GetMessage(1001) // "Invalid operator ID or password"
                    });
                }

                // 4. Success  Reset attempts
                _loginAttemptService.ResetAttempts(key);

                // Generate JWT
                var token = await _jwtService.GenerateTokenAsync(request.OperatorId, loginResult.User.OperatorRole);

                // Generate IST timestamp
                TimeZoneInfo indianZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indianZone);

                var jwtSettings = _configuration.GetSection("JwtSettings");
                double.TryParse(jwtSettings["DurationInMinutes"], out var durationMins);
                var expiryDuration = durationMins > 0 ? durationMins : 540;


                // Cookie settings
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(expiryDuration),
                    Path = "/"
                };

                Response.Cookies.Append("AuthToken", token, cookieOptions);

                // Log success
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
                await _systemLogger.LogAsync(
                    "LogInController",
                    $"LoginEmployee-Success ({env})",
                    $"OperatorId: {request.OperatorId}, Role: {loginResult.User.OperatorRole}, Time: {indianTime}");

                return Ok(new
                {
                    message = _userMessageService.GetMessage(2001),
                    isTempPassword = loginResult.IsTempPassword,
                    expires = DateTime.UtcNow.AddMinutes(expiryDuration),
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