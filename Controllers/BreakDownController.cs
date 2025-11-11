using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.BreakDownDto;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        [EnableRateLimiting("GeneralRateLimit")]
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] BreakDownStartRequest request)
        {
            try
            {
                var result = await _repo.StartBreakDownAsync(request);

                var response = new
                {
                    success = result.IsDbSuccess,
                    message = result.Message, // Already set via helper in repo
                    dbStatus = result.IsDbSuccess,
                    mailStatus = result.IsMailSent,
                    sapStatus = result.IsSapPosted
                };

                if (!result.IsDbSuccess)
                    return BadRequest(response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakDownController", "Start", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    dbStatus = false,
                    mailStatus = false,
                    sapStatus = false
                });
            }
        }

        [EnableRateLimiting("GeneralRateLimit")]
        [HttpPost("end")]
        public async Task<IActionResult> EndBreakdown([FromBody] BreakDownEndRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NOTIF_NUM))
                    return BadRequest(new
                    {
                        success = false,
                        message = "Notification number is required.",
                        dbStatus = false,
                        mailStatus = false,
                        sapStatus = false
                    });

                var result = await _repo.EndBreakDownAsync(request.NOTIF_NUM);

                return Ok(new
                {
                    success = result.IsDbSuccess,
                    message = result.Message,
                    dbStatus = result.IsDbSuccess,
                    mailStatus = result.IsMailSent,
                    sapStatus = result.IsSapPosted
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakDownController", "End", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    dbStatus = false,
                    mailStatus = false,
                    sapStatus = false
                });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllBreakdowns()
        {
            try
            {
                var data = await _repo.GetAllBreakDownsAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("BreakDownController", "GetAllBreakdowns", ex.ToString());
                return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(5001) });
            }
        }

    }
}
