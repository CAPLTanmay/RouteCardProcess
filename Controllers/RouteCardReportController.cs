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
        public async Task<IActionResult> GetNavLossByIdPost([FromBody] OrderReportRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SetupId) && string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = "Either SetupId or MachiningId must be provided." });

                var result = await _repo.GetLossOrderByIdsAsync(request);
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
        [HttpPost("get-exception-report")]
        public async Task<IActionResult> GetExceptionReport([FromBody] OrderReportRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SetupId) && string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = "Either SetupId or MachiningId must be provided." });

                var result = await _repo.GetExceptionReportAsync(request);
                if (result == null)
                    return NotFound(new { message = "No exception data found for the provided ID(s)." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("RouteCardReportController", "get-exception-report", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("combined-order-report")]
        public async Task<IActionResult> GetCombinedOrderReport([FromBody] OrderReportRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SetupId) && string.IsNullOrWhiteSpace(request.MachiningId))
                    return BadRequest(new { message = "Either SetupId or MachiningId must be provided." });
                var timingInfoTask = _repo.GetTimingInfoAsync(request);
                var navLossTask = _repo.GetLossOrderByIdsAsync(request);
                var exceptionReportTask = _repo.GetExceptionReportAsync(request);
            

                await Task.WhenAll(timingInfoTask,navLossTask, exceptionReportTask);

                var response = new CombinedOrderReportResponseDto
                {
                    TimingInfo = timingInfoTask.Result,
                    NavLossData = navLossTask.Result,
                    ExceptionReportData = exceptionReportTask.Result                    
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("RouteCardReportController", "combined-order-report", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }



        [HttpPost("update-all")]
        public async Task<IActionResult> UpdateAll([FromBody] FullUpdateDto dto)
        {
            if (dto.Setup == null && dto.Machining == null)
                return BadRequest(new { Message = "At least one of Setup or Machining must be provided." });

            try
            {
                if (dto.Setup != null)
                {
                    if (dto.Setup.SetupStartTime.HasValue || dto.Setup.SetupEndTime.HasValue)
                        await _repo.UpdateSetupTimesAsync(dto.Setup);

                    if (dto.Setup.IdleTimes?.Any() == true)
                        await _repo.UpdateIdleTimesAsync(dto.Setup.SetUpID, dto.Setup.UpdatedOperatorId, dto.Setup.IdleTimes);

                    if (dto.Setup.ExceptionTimes?.Any() == true)
                        await _repo.UpdateExceptionTimesAsync(dto.Setup.SetUpID, dto.Setup.UpdatedOperatorId, dto.Setup.ExceptionTimes);
                }

                if (dto.Machining != null)
                {
                    if (dto.Machining.MachiningStartTime.HasValue || dto.Machining.MachiningEndTime.HasValue)
                        await _repo.UpdateMachiningTimesAsync(dto.Machining);

                    if (dto.Machining.IdleTimes?.Any() == true)
                        await _repo.UpdateMachiningIdleTimesAsync(dto.Machining.MachiningId, dto.Machining.UpdatedOperatorId, dto.Machining.IdleTimes);

                    if (dto.Machining.ExceptionTimes?.Any() == true)
                        await _repo.UpdateMachiningExceptionTimesAsync(dto.Machining.MachiningId, dto.Machining.UpdatedOperatorId, dto.Machining.ExceptionTimes);

                    if (dto.Machining.OperatorQuantities?.Any() == true)
                        await _repo.UpdateMachiningOperatorQuantitiesAsync(dto.Machining.MachiningId, dto.Machining.OperatorQuantities);
                }

                return Ok(new { Message = "All applicable updates applied successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
