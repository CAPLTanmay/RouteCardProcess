using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Manualdata;
using RouteCardProcess.Model.DTOs.RouteCardReport;
using RouteCardProcess.Model.DTOs.SapValidation;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManualDataController : ControllerBase
    {
        private readonly IManualDataRepository _manualDataRepository;
        private readonly IRouteCardReportRepository _repo;
        private readonly IUserMessageService _userMessageService;
        private readonly ISystemLoggerRepository _logger;

        public ManualDataController(
            IManualDataRepository manualDataRepository,
            IRouteCardReportRepository repo,
            IUserMessageService userMessageService,
            ISystemLoggerRepository logger)
        {
            _manualDataRepository = manualDataRepository;
            _repo = repo;
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

                message = result.Message ??
                  (result.Success
                      ? "Data updated successfully"
                      : "No record found to update"),
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

        [HttpPost("get-uploaded-manual-report")]
        public async Task<IActionResult> GetUploadedManualReport([FromBody] RouteCardReportFilterRequest request)
        {
            try
            {
                var result = await _manualDataRepository.GetUploadedManualReportAsync(request);
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

        [HttpPost("manual-order-report")]
        public async Task<IActionResult> GetCombinedOrderReport([FromBody] OrderReportRequestDto request)
        {
            try
            {
                // Validation - at least one identifier must be provided
                if (string.IsNullOrWhiteSpace(request.SetupId) &&
                     string.IsNullOrWhiteSpace(request.MachiningId) &&
                    (!request.OperatorTransactionId.HasValue || request.OperatorTransactionId == Guid.Empty) &&
                    (!request.MachiningOperatorTransactionId.HasValue || request.MachiningOperatorTransactionId == Guid.Empty))
                {
                    return BadRequest(new
                    {
                        message = "At least one of SetupId, MachiningId, OperatorTransactionId, or MachiningOperatorTransactionId must be provided."
                    });
                }

                var timingInfoTask = _manualDataRepository.GetManualTimingInfo(request);
                if (timingInfoTask != null)
                {
                    request.ReqOperatorId = timingInfoTask.Result.SetupOperatorId
                      ?? timingInfoTask.Result.MachiningOperatorId;
                }
                var navLossTask = _repo.GetLossOrderByIdsAsync(request);
                var exceptionReportTask = _repo.GetExceptionReportAsync(request);


                await Task.WhenAll(timingInfoTask, navLossTask, exceptionReportTask);

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
                await _logger.LogAsync("RouteCardReportController", "combined-order-report", ex.ToString());
                return StatusCode(500, new { message = _userMessageService.GetMessage(5005), error = ex.Message });
            }
        }

        [HttpPost("confirm-manual-data")]
        public async Task<IActionResult> ConfirmProductionOrder([FromBody] CombinedSAPConfirmationRequest request)
        {
            try
            {
                var response = await _manualDataRepository.ConfirmManualOrderAsync(request);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1070),
                    data = JsonSerializer.Deserialize<object>(response)
                });
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await _logger.LogAsync("ValidationController", "confirmProductionOrder", ex.ToString());

                string sapMessage = ValidationRepository.ExtractSapErrorMessage(ex.Message);

                return BadRequest(new
                {
                    success = false,
                    message = sapMessage
                });
            }
            catch (HttpRequestException ex)
            {
                await _logger.LogAsync("ValidationController", "confirmProductionOrder", ex.ToString());
                return StatusCode(502, new { success = false, message = _userMessageService.GetMessage(5002), details = ex.Message });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("ValidationController", "confirmProductionOrder", ex.ToString());
                return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(5001), details = ex.Message });
            }
        }
    }
}
