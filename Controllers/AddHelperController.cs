using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Helper;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddHelperController : ControllerBase
    {
        private readonly IHelperRepository _helperRepository;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public AddHelperController(IHelperRepository helperRepository, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService, ITokenBlacklistService tokenBlacklistService)
        {
            _helperRepository = helperRepository;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
            _tokenBlacklistService = tokenBlacklistService;
        }

        [HttpPost("add-helper")]
        public async Task<IActionResult> AddHelper([FromBody] HelperRequest request)
        
        {
            try
            {
                var result = await _helperRepository.AddHelperAsync(request);

                if (result == _userMessageService.GetMessage(1007))
                    return Ok(new { message = result });

                return BadRequest(new { message = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("AddHelperController", "add-helper", ex.ToString());
                var message = _userMessageService.GetMessage(5001);
                return StatusCode(500, message);
            }
        }

        [HttpPost("end-helper")]
        public async Task<IActionResult> EndHelper([FromBody] EndHelperRequest request)
        {
            try
            {
                var result = await _helperRepository.EndHelperAsync(request);

                if (result == _userMessageService.GetMessage(1008))
                    return Ok(new { message = result });

                return BadRequest(new { message = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("AddHelperController", "end-helper", ex.ToString());
                var message = _userMessageService.GetMessage(5001);
                return StatusCode(500, message);
            }
        }

        [HttpPost("toggle-helper-pause")]
        public async Task<IActionResult> ToggleHelperPause([FromBody] EndHelperRequest request)
        {
            try
            {
                var result = await _helperRepository.ToggleHelperPauseAsync(request);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("AddHelperController", "toggle-helper-pause", ex.ToString());
                var message = _userMessageService.GetMessage(5001);
                return StatusCode(500, message);
            }
        }

        [HttpPost("helpers")]
        public async Task<IActionResult> GetHelpersByMainOperatorId([FromBody] MainOperatorRequestDto request)
        {
            try
            {
                var helpers = await _helperRepository.GetHelpersByMainOperatorIdAsync(request.MainOperatorId);
                return Ok(helpers);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("AddHelperController", "helpers", ex.ToString());
                var message = _userMessageService.GetMessage(5001);
                return StatusCode(500, message);
            }
        }

        [Authorize]
        [HttpPost("check-helper-before-logout")]
        public async Task<IActionResult> CheckHelperBeforeLogout([FromBody] MainOperatorRequestDto request)
        {
            try
            {
                //  STEP 1: Check if helpers are attached
                var helpers = await _helperRepository.GetHelpersByMainOperatorIdAsync(request.MainOperatorId);
                if (helpers != null && helpers.Any())
                {
                    return Ok(new
                    {
                        status = false,
                        message = _userMessageService.GetMessage(1100), // "Detach helpers before logout"
                        data = helpers
                    });
                }

                //  STEP 2: Extract JWT claims from current user
                var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var expUnix = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

                if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(expUnix))
                {
                    return Unauthorized(new { status = false, message = "Invalid or missing token claims" });
                }

                var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expUnix)).UtcDateTime;

                //  STEP 3: Blacklist token
                await _tokenBlacklistService.RevokeTokenAsync(jti, exp);

                //  STEP 4: Delete cookie from browser
                Response.Cookies.Delete("AuthToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

                //  STEP 5: Return unified success
                return Ok(new
                {
                    status = true,
                    message = _userMessageService.GetMessage(1101), // "Logout successful"
                    tokenRevoked = true
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("AddHelperController", "check-helper-before-logout", ex.ToString());
                var message = _userMessageService.GetMessage(5001); // "Internal error"
                return StatusCode(500, new { status = false, message });
            }
        }

        [HttpPost("release-all-helpers")]
        public async Task<IActionResult> ReleaseAllHelpers([FromBody] MainOperatorRequestDto request)
        {
            try
            {
                // Step 1: Get all active helpers
                var activeHelpers = await _helperRepository.GetHelpersByMainOperatorIdAsync(request.MainOperatorId);

                if (activeHelpers == null || !activeHelpers.Any())
                {
                    return Ok(new { message = _userMessageService.GetMessage(1102) });
                }

                // Step 2: Loop through and release each helper
                foreach (var helper in activeHelpers)
                {
                    var endHelperRequest = new EndHelperRequest
                    {
                        OperatorId = helper.OperatorId, 
                        SetupId = helper.SetupId,
                        MachiningId = helper.MachiningId
                    };

                    var result = await _helperRepository.EndHelperAsync(endHelperRequest);

                    if (result != _userMessageService.GetMessage(1008))
                    {
                        string messageTemplate = _userMessageService.GetMessage(1103);
                        string formattedMessage = messageTemplate
                            .Replace("{OperatorId}", helper.OperatorId.ToString())
                            .Replace("{result}", result);
                        return BadRequest(new { message = formattedMessage });
                    }
                }

                return Ok(new { message = _userMessageService.GetMessage(1104) });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("AddHelperController", "release-all-helpers", ex.ToString());
                var message = _userMessageService.GetMessage(5001);
                return StatusCode(500, message);
            }
        }
    }
}
