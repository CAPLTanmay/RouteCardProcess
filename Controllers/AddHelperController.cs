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

    }
}
