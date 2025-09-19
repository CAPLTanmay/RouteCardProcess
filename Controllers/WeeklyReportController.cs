using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.WeeklyReport;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WeeklyReportController : ControllerBase
    {
        private readonly IWeeklyReportRepository _repo;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;

        public WeeklyReportController(IWeeklyReportRepository repo, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _repo = repo;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
        }

        [HttpPost("get-exception")]
        public async Task<IActionResult> GetExceptionReport([FromBody] WeeklyReportRequestDto request)
        {
            try
            {
                if (request.FromDate == default || request.ToDate == default)
                    return BadRequest(new { message = "FromDate and ToDate are required." });

                var result = await _repo.GetExceptionReportAsync(request);

                if (result == null || !result.Any())
                {
                    var msg = _userMessageService.GetMessage(1063) ?? "No data found";
                    await _systemLogger.LogAsync("WeeklyReportController", "get-exception", "No data for given filters.");
                    return Ok(new { success = false, message = msg, data = new List<object>() });
                }

                return Ok(new { success = true, message = "Data fetched successfully", data = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("WeeklyReportController", "get-exception", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("get-without-exception")]
        public async Task<IActionResult> GetWithoutExceptionReport([FromBody] WeeklyReportRequestDto request)
        {
            try
            {
                if (request.FromDate == default || request.ToDate == default)
                    return BadRequest(new { message = "FromDate and ToDate are required." });

                var result = await _repo.GetWithoutExceptionReportAsync(request);

                if (result == null || !result.Any())
                {
                    var msg = _userMessageService.GetMessage(1063) ?? "No data found";
                    await _systemLogger.LogAsync("WeeklyReportController", "get-exception", "No data for given filters.");
                    return Ok(new { success = false, message = msg, data = new List<object>() });
                }

                return Ok(new { success = true, message = "Data fetched successfully", data = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("WeeklyReportController", "get-exception", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("get-idelcode")]
        public async Task<IActionResult> GetIdelCodeReport([FromBody] WeeklyReportRequestDto request)
        {
            try
            {
                if (request.FromDate == default || request.ToDate == default)
                    return BadRequest(new { message = "FromDate and ToDate are required." });

                var result = await _repo.GetIdelCodeReportAsync(request);

                if (result == null || !result.Any())
                {
                    var msg = _userMessageService.GetMessage(1063) ?? "No data found";
                    await _systemLogger.LogAsync("WeeklyReportController", "get-exception", "No data for given filters.");
                    return Ok(new { success = false, message = msg, data = new List<object>() });
                }

                return Ok(new { success = true, message = "Data fetched successfully", data = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("WeeklyReportController", "get-exception", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("get-associate")]
        public async Task<IActionResult> GetAssociateCounts()
        {
            try
            {
                var result = await _repo.GetAssociateCountsAsync();

                if (result == null || !result.Any())
                {
                    var msg = _userMessageService.GetMessage(1063) ?? "No data found";
                    await _systemLogger.LogAsync("WeeklyReportController", "get-associate", "No data found in MSTDepartmentCounts.");
                    return Ok(new { success = false, message = msg, data = new List<object>() });
                }

                return Ok(new { success = true, message = "Data fetched successfully", data = result });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("WeeklyReportController", "get-associate", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

    }
}
