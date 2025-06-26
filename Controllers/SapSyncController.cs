using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.SapValidation;
using RouteCardProcess.Repositories;

[ApiController]
[Route("api/sync")]
[Authorize]
public class SapSyncController : ControllerBase
{
    private readonly ISapSyncService _sapSyncService;
    private readonly IUserMessageService _userMessageService;
    private readonly ISystemLoggerRepository _logger;

    public SapSyncController(ISapSyncService sapSyncService, IUserMessageService userMessageService, ISystemLoggerRepository logger)
    {
        _sapSyncService = sapSyncService;
        _userMessageService = userMessageService;
        _logger = logger;
    }

    [HttpPost("routing")]
    public async Task<IActionResult> SyncRoutingData([FromBody] RoutingDataRequest request)
    {
        try
        {
            await _sapSyncService.SyncRoutingDataAsync(request.OrderNumber);

            return Ok(new
            {
                success = true,
                message = _userMessageService.GetMessage(1097) 
            });
        }
        catch (Exception ex)
        {
            await _logger.LogAsync("SapSyncController", "SyncRoutingData", ex.ToString());

            return StatusCode(500, new
            {
                success = false,
                message = _userMessageService.GetMessage(5001),
                details = ex.Message
            });
        }
    }
    [HttpPost("routing-data")]
    public async Task<IActionResult> GetRoutingDataByOrderNumber([FromBody] RoutingDataRequest request)
    {
        try
        {
            var data = await _sapSyncService.GetSelectedRoutingDataAsync(request.OrderNumber);

            if (data == null || !data.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1099)
                });
            }

            return Ok(new
            {
                success = true,
                message = _userMessageService.GetMessage(1098),
                data
            });
        }
        catch (Exception ex)
        {
            await _logger.LogAsync("SapSyncController", "GetRoutingDataByOrderNumber", ex.ToString());

            return StatusCode(500, new
            {
                success = false,
                message = _userMessageService.GetMessage(5001),
                details = ex.Message
            });
        }
    }

}
