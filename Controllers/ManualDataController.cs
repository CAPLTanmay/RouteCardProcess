using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Machining;
using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.RouteCardReport;
using RouteCardProcess.Model.DTOs.Setup;
using RouteCardProcess.Repositories;
using System.Threading.Tasks;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManualDataController : ControllerBase
    {
        private readonly IManualDataRepository _manualDataRepository;
        private readonly IUserMessageService _userMessageService;
        private readonly ISystemLoggerRepository _logger;

        public ManualDataController(
            IManualDataRepository manualDataRepository,
            IUserMessageService userMessageService,
            ISystemLoggerRepository logger)
        {
            _manualDataRepository = manualDataRepository;
            _userMessageService = userMessageService;
            _logger = logger;
        }

        //  SYNC FROM SAP 
        [HttpPost("sync-manual-routing")]
        public async Task<IActionResult> SyncRoutingData([FromBody] MaualDataRequest request)
        {
            try
            {
                await _manualDataRepository.SyncManualDataAsync(request);
                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1097)
                });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("ManualDataController", "SyncRoutingData", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    details = ex.Message
                });
            }
        }

        // GET MANUAL DATA 
        [HttpPost("get-manual-data")]
        public async Task<IActionResult> GetRoutingDataByOrderNumber([FromBody] GetMaualDataRequest request)
        {
            try
            {
                var data = await _manualDataRepository.GetManualDataAsync(request);

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
                await _logger.LogAsync("ManualDataController", "GetRoutingDataByOrderNumber", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    details = ex.Message
                });
            }
        }

        // UPDATE MANUAL DATA 
        [HttpPost("update-manual-data")]
        public async Task<IActionResult> UpdateManualData([FromBody] ManualDataUpdateDto dto)
        {
            var result = await _manualDataRepository.UpdateManualDataAsync(dto);

            return Ok(new
            {
                success = result.Success,
                message = result.Success ? "Data updated successfully" : "No record found to update",
                setupId = result.SetupId,
                machiningId = result.MachiningId,
                opertorid = result.OperatorId
            });
        }

        //ADD DELAYS
        [HttpPost("add-setup-delays")]
        public async Task<IActionResult> AddDelays([FromBody] ManualSetupDelayRequest request)
        {
            try
            {
                var result = await _manualDataRepository.InsertDelaysAsync(request);

                return result
                    ? Ok(new { message = _userMessageService.GetMessage(1034) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1035) });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("ManualDataController", "add-delays", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("add-machining-delays")]
        public async Task<IActionResult> AddDelays([FromBody] ManualMachiningDelayRequest request)
        {
            try
            {
                var result = await _manualDataRepository.AddDelaysAsync(request);

                return result
                    ? Ok(new { message = _userMessageService.GetMessage(1034) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1035) });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("ManualDataController", "add-delays", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("get-manual-report")]
        public async Task<IActionResult> GetRouteCardReportFiltered([FromBody] RouteCardReportFilterRequest request)
        {
            try
            {
                var result = await _manualDataRepository.GetManualReportAsync(request);
                if (result == null || !result.Any())
                {
                    var message = _userMessageService.GetMessage(1063) ?? "No data found";
                    await _logger.LogAsync("RouteCardReportController", "get-filtered", "No data found for given filters.");
                    return Ok(new { success = false, message, data = new List<object>() });
                }

                return Ok(new { success = true, message = "Data fetched successfully", data = result });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("ManualDataController", "get-filtered", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }
    }
}
