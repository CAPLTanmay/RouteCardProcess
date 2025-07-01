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
    [Authorize]
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
        public async Task<IActionResult> ConfirmProductionOrder([FromBody] ProductionOrderConfirmationRequest request)
        {
            try
            {
                var response = await _repo.ConfirmProductionOrderAsync(request);

                return Ok(new
                {
                    success = true,
                    message = _userMessageService.GetMessage(1070),
                    data = JsonSerializer.Deserialize<object>(response)
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

    }
}
