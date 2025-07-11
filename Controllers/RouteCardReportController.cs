using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.RouteCardReport;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class RouteCardReportController : ControllerBase
    {
        private readonly IRouteCardReportRepository _repo;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;

        public RouteCardReportController(IRouteCardReportRepository repo, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _repo = repo;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
        }

        [HttpPost("get-by-workorder")]
        public async Task<IActionResult> GetRouteCardReport([FromBody] WorkOrderRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.WorkOrderNo))
                    return BadRequest(new { message = _userMessageService.GetMessage(1062) });

                var result = await _repo.GetRouteCardReportAsync(request.WorkOrderNo);
                if (result == null || !result.Any())
                    return NotFound(new { message = _userMessageService.GetMessage(1063) });

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("RouteCardReportController", "get-by-workorder", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("get-report")]
        public async Task<IActionResult> GetRouteCardReportFiltered([FromBody] RouteCardReportFilterRequest request)
        {
            try
            {
                var result = await _repo.GetRouteCardReportFilteredAsync(request);
                if (result == null || !result.Any())
                    return NotFound(new { message = _userMessageService.GetMessage(1063) });

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("RouteCardReportController", "get-filtered", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("loss-order-report")]
        public async Task<IActionResult> GetNavLossByIdPost([FromBody] LossOrderRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SetupId) && string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = "Either SetupId or MachiningId must be provided." });

                var result = await _repo.GetLossOrderByIdsAsync(request.SetupId, request.MachiningId);
                if (result == null)
                    return NotFound(new { message = "No NAV loss data found for the provided ID." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("RouteCardReportController", "nav-loss-post-id", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

    }
}
