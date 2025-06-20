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

        public AddHelperController(IHelperRepository helperRepository, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _helperRepository = helperRepository;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
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

        [HttpPost("check-helper-before-logout")]
        public async Task<IActionResult> CheckHelperBeforeLogout([FromBody] MainOperatorRequestDto request)
        {
            try
            {
                var helpers = await _helperRepository.GetHelpersByMainOperatorIdAsync(request.MainOperatorId);

                if (helpers != null && helpers.Any())
                {
                    return Ok(new
                    {
                        status = false,
                        message = "Some helpers are still active. Please release them before logging out.",
                        data = helpers
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "No active helpers. Safe to logout."
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("AddHelperController", "check-helper-before-logout", ex.ToString());
                var message = _userMessageService.GetMessage(5001);
                return StatusCode(500, message);
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
                    return Ok(new { message = "No active helpers found to release." });
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
                        return BadRequest(new { message = $"Failed to release helper {helper.OperatorId}: {result}" });
                    }
                }

                return Ok(new { message = "All helpers released successfully." });
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
