using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.SapValidation;
using RouteCardProcess.Repositories;


namespace RouteCardProcess.Controllers
{
    public class ManualDataController : ControllerBase
    {
        private readonly IManualDataRepository _maualDataRepository;
        private readonly IUserMessageService _userMessageService;
        private readonly ISystemLoggerRepository _logger;

        public ManualDataController(IManualDataRepository maualDataRepository, IUserMessageService userMessageService, ISystemLoggerRepository logger)
        {
            _maualDataRepository = maualDataRepository;
            _userMessageService = userMessageService;
            _logger = logger;
        }

        [HttpPost("sync-manual-routing")]
        public async Task<IActionResult> SyncRoutingData([FromBody] MaualDataRequest request)
        {
            try
            {
                await _maualDataRepository.SyncManualDataAsync(request);

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
        [HttpPost("get-manual-data")]
        public async Task<IActionResult> GetRoutingDataByOrderNumber([FromBody] GetMaualDataRequest request)
        {
            try
            {
                var data = await _maualDataRepository.GetManualDataAsync(request);

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
}
