using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.BreakDownDto;
using Microsoft.Extensions.Logging;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class BreakDownController : ControllerBase
    {
        private readonly IBreakDownRepository _repo;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;
        public BreakDownController(IBreakDownRepository repo, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _repo = repo;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] BreakDownStartRequest request)
        {
            try
            {
                var result = await _repo.StartBreakDownAsync(request);

                if (!result.IsDbSuccess)
                    return BadRequest(new { message = _userMessageService.GetMessage(1015), dbStatus = false, mailStatus = result.IsMailSent, sapStatus = result.IsSapPosted });

                return Ok(new { message = result.Message, dbStatus = result.IsDbSuccess, mailStatus = result.IsMailSent, sapStatus = result.IsSapPosted });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakDownController", "Start", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), dbStatus = false, mailStatus = false, sapStatus = false });
            }
        }


        [HttpPost("end")]
        public async Task<IActionResult> End([FromBody] BreakDownEndRequest request)
        {
            try
            {
                var success = await _repo.EndBreakDownAsync(request.WorkCenterNo, request.OperatorId, request.BreakDownReasonCode);
                if (success)
                    return Ok(new { message = _userMessageService.GetMessage(1013) });
                else
                    return NotFound(new { message = _userMessageService.GetMessage(1014) });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakDownController", "end", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001) });
            }
        }
    }
}
