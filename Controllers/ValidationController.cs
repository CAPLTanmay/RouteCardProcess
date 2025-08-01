using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.SapValidation;
using RouteCardProcess.Repositories;
using System.Text.Json;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class ValidationController : ControllerBase
    {
        private readonly IValidationRepository _repo;
        private readonly IJwtTokenService _jwtService;
        private readonly ISystemLoggerRepository _systemLogger;
        private readonly IUserMessageService _userMessageService;
        public ValidationController(IValidationRepository repo, IJwtTokenService jwtService, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
        {
            _repo = repo;
            _jwtService = jwtService;
            _systemLogger = systemLogger;
            _userMessageService = userMessageService;
        }

        [HttpPost("validate-workcenter")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateWorkCenter([FromBody] WorkCenterRequest request)
        {
            try
            {
                var resultJson = await _repo.ValidateWorkCenterAsync(request.WorkCenter);

                var data = JsonSerializer.Deserialize<object>(resultJson);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1064),
                    data = data
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("ValidationController", "validate-workcenter", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1107),
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("ValidationController", "validate-workcenter", ex.ToString());
                return StatusCode(502, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5002),
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "validate-workcenter", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    details = ex.Message
                });
            }
        }

        [HttpPost("validate-order")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateOrder([FromBody] ValidateOrderRequest request)
        {
            try
            {
                var resultJson = await _repo.ValidateOrderAsync(request.Order, request.WorkCenter);
                var data = JsonSerializer.Deserialize<object>(resultJson);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1065),
                    data = data
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("ValidationController", "validate-order", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1107),
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("ValidationController", "validate-order", ex.ToString());
                return StatusCode(502, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5002),
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "validate-order", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    details = ex.Message
                });
            }
        }

        [HttpPost("routing-data")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoutingData([FromBody] RoutingDataRequest request)
        {
            try
            {
                var resultJson = await _repo.GetRoutingDataAsync(request.OrderNumber);

                var data = JsonSerializer.Deserialize<object>(resultJson);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1066),
                    data = data
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("ValidationController", "routing-data", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1107),
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("ValidationController", "routing-data", ex.ToString());
                return StatusCode(502, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5002),
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "routing-data", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    details = ex.Message
                });
            }
        }

        [HttpGet("loss-data")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLossData()
        {
            try
            {
                var resultJson = await _repo.GetLossDataAsync();

                var data = JsonSerializer.Deserialize<object>(resultJson);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1067),
                    data = data
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("ValidationController", "loss-data", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1107),
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("ValidationController", "loss-data", ex.ToString());
                return StatusCode(502, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5002),
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "loss-data", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    details = ex.Message
                });
            }
        }

        [HttpGet("maintenance-notifications")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMaintenanceNotifications()
        {
            try
            {
                var resultJson = await _repo.GetMaintenanceNotificationsAsync();

                var data = JsonSerializer.Deserialize<object>(resultJson);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1068),
                    data = data
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("ValidationController", "maintenance-notifications", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1107),
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("ValidationController", "maintenance-notifications", ex.ToString());
                return StatusCode(502, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5002),
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "maintenance-notifications", ex.ToString());
                return StatusCode(500, new
                {
                    success = false,
                    message = _userMessageService.GetMessage(5001),
                    details = ex.Message
                });
            }
        }

        [HttpPost("updateWorkCenter")]
        public async Task<IActionResult> UpdateWorkCenter([FromBody] WorkCenterUpdateRequest request)
        {
            try
            {
                var response = await _repo.UpdateWorkCenterAsync(request);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1069),
                    data = JsonSerializer.Deserialize<object>(response)
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("ValidationController", "updateWorkCenter", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1107),
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("ValidationController", "updateWorkCenter", ex.ToString());
                return StatusCode(502, new { success = false, message = _userMessageService.GetMessage(5002), details = ex.Message });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "updateWorkCenter", ex.ToString());
                return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(5001), details = ex.Message });
            }
        }

        [HttpPost("confirmProductionOrder")]
        public async Task<IActionResult> ConfirmProductionOrder([FromBody] CombinedSAPConfirmationRequest request)
        {
            try
            {
                var response = await _repo.ConfirmProductionOrderAsync(request); // returns SapResponseDto

                return Ok(new
                {
                    success = !response.IsError,
                    message = response.IsError
                                ? string.Join(" | ", response.Messages)
                                : _userMessageService.GetMessage(1070),
                    data = response // already object, no need to deserialize
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("ValidationController", "confirmProductionOrder", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1107),
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("ValidationController", "confirmProductionOrder", ex.ToString());
                return StatusCode(502, new { success = false, message = _userMessageService.GetMessage(5002), details = ex.Message });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "confirmProductionOrder", ex.ToString());
                return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(5001), details = ex.Message });
            }
       

        }

        [HttpPost("confirmLossOrder")]
        public async Task<IActionResult> ConfirmLossOrder([FromBody] LossOrderSapRequest request)
        {
            try
            {
                var response = await _repo.ConfirmLossOrderAsync(request);
                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1071), 
                    data = JsonSerializer.Deserialize<object>(response)
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("LossOrderController", "confirmLossOrder", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = _userMessageService.GetMessage(1108),
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("LossOrderController", "confirmLossOrder", ex.ToString());
                return StatusCode(502, new { success = false, message = _userMessageService.GetMessage(5002), details = ex.Message });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("LossOrderController", "confirmLossOrder", ex.ToString());
                return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(5001), details = ex.Message });
            }
        }

        [HttpPost("confirmCombinedOrder")]
        public async Task<IActionResult> ConfirmCombinedOrder([FromBody] CombinedConfirmationRequest request)
        {
            try
            {
                var (productionResponse, lossResponse) = await _repo.ConfirmCombinedOrderAsync(request);

                return Ok(new
                {
                    success = true,
                    message = "Both production and loss orders confirmed successfully.",
                    production = productionResponse,
                    loss = lossResponse
                });
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("400"))
            {
                await _systemLogger.LogAsync("ValidationController", "confirmCombinedOrder", ex.ToString());
                return BadRequest(new
                {
                    success = false,
                    message = "Bad request during confirmation.",
                    details = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                await _systemLogger.LogAsync("ValidationController", "confirmCombinedOrder", ex.ToString());
                return StatusCode(502, new { success = false, message = "SAP gateway unavailable", details = ex.Message });
            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "confirmCombinedOrder", ex.ToString());
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }

        [HttpPost("confirmProdAndLossOrder")]
        public async Task<IActionResult> ConfirmProdAndLossOrder([FromBody] CombinedSAPConfirmationRequest request)
        {
            try
            {
                var (prodResponse, lossResponse) = await _repo.ConfirmProdAndLossOrderAsync(request);

                bool prodSuccess = prodResponse is SapResponseDto p && !p.IsError;
                bool lossSuccess = lossResponse is SapResponseDto l && !l.IsError;

                string message = prodSuccess && lossSuccess
                    ? "Production and Loss Orders confirmed successfully."
                    : prodSuccess ? "Production confirmed. Loss not processed or failed."
                    : "Production order confirmation failed.";

                return Ok(new
                {
                    success = prodSuccess,
                    message,
                    production = prodResponse,
                    loss = lossResponse
                });

            }
            catch (Exception ex)
            {
                await _systemLogger.LogAsync("ValidationController", "confirmProdAndLossOrder", ex.ToString());
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }


    }
}
