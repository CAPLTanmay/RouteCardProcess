using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.SapValidation;
using System.Text.Json;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidationController : ControllerBase
    {
        private readonly IValidationRepository _repo;
        private readonly IJwtTokenService _jwtService;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(IValidationRepository repo, IJwtTokenService jwtService, ILogger<ValidationController> logger)
        {
            _repo = repo;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpGet("validate-workcenter/{workCenter}")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateWorkCenter(string workCenter)
        {
            try
            {
                var resultJson = await _repo.ValidateWorkCenterAsync(workCenter);

                // Deserialize the JSON string to object so we can wrap it
                var data = JsonSerializer.Deserialize<object>(resultJson);

                return Ok(new
                {
                    success = true,
                    message = "Work center validated successfully",
                    data = data
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error in ValidateWorkCenter");
                return StatusCode(502, new
                {
                    success = false,
                    message = "External service error",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateWorkCenter");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    details = ex.Message
                });
            }
        }

        [HttpGet("validate-order/{order}/{workCenter}")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateOrder(string order, string workCenter)
        {
            try
            {
                var resultJson = await _repo.ValidateOrderAsync(order, workCenter);

                // Deserialize raw JSON from SAP to object so it can be wrapped
                var data = JsonSerializer.Deserialize<object>(resultJson);

                return Ok(new
                {
                    success = true,
                    message = "Work Order validated successfully",
                    data = data
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error in ValidateOrder");
                return StatusCode(502, new
                {
                    success = false,
                    message = "External service error",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateOrder");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    details = ex.Message
                });
            }
        }

        [HttpGet("routing-data/{orderNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoutingData(string orderNumber)
        {
            try
            {
                var resultJson = await _repo.GetRoutingDataAsync(orderNumber);

                var data = JsonSerializer.Deserialize<object>(resultJson);

                return Ok(new
                {
                    success = true,
                    message = "Routing data fetched successfully",
                    data = data
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error in GetRoutingData");
                return StatusCode(502, new
                {
                    success = false,
                    message = "External service error",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRoutingData");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
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
                    message = "Loss data fetched successfully",
                    data = data
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error in GetLossData");
                return StatusCode(502, new
                {
                    success = false,
                    message = "External service error",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLossData");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
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
                    message = "Maintenance notifications fetched successfully",
                    data = data
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error in GetMaintenanceNotifications");
                return StatusCode(502, new
                {
                    success = false,
                    message = "External service error",
                    details = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMaintenanceNotifications");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
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
                    message = "Work center updated successfully",
                    data = JsonSerializer.Deserialize<object>(response)
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error in UpdateWorkCenter");
                return StatusCode(502, new { success = false, message = "External service error", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateWorkCenter");
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
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
                    message = "Production order confirmed successfully",
                    data = JsonSerializer.Deserialize<object>(response)
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error in ConfirmProductionOrder");
                return StatusCode(502, new { success = false, message = "External service error", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmProductionOrder");
                return StatusCode(500, new { success = false, message = "Internal server error", details = ex.Message });
            }
        }

    }
}
