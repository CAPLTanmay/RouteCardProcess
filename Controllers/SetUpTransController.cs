using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Setup;
using Microsoft.Extensions.Logging;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SetUpTransController : ControllerBase
    {
        private readonly ISetUpTransRepository _repo;
        private readonly ILogger<SetUpTransController> _logger;

        public SetUpTransController(ISetUpTransRepository repo, ILogger<SetUpTransController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpPost("check-status")]
        public async Task<IActionResult> CheckStatus([FromBody] SetupMasterDto request)
        {
            try
            {
                var result = await _repo.CheckSetupNotificationStatusAsync(
                    request.WorkCenterNo,
                    request.WorkOrderNo,
                    request.OperationNo
                );

                return Ok(new
                {
                    flag = result.Flag,
                    setupStatus = result.SetupStatus,
                    machiningStatus = result.MachiningStatus,
                    setupId = result.SetUpID,
                    machiningId = result.MachiningID,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking setup status.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpPost("check-or-create")]
        public async Task<IActionResult> CheckOrCreateSetup([FromBody] SetupMasterDto request)
        {
            try
            {
                var existing = await _repo.GetByCompositeKeyAsync(request.WorkCenterNo, request.WorkOrderNo, request.OperationNo);

                if (existing != null)
                {
                    var startTime = existing.SetupStartTime;
                    var endTime = existing.SetupEndTime ?? DateTime.Now;

                    TimeSpan? adjustedTotalSetupTime = null;

                    if (startTime.HasValue)
                        adjustedTotalSetupTime = endTime - startTime;

                    return Ok(new
                    {
                        message = "Setup already exists",
                        SetUpID = existing.SetUpID,
                        setup = existing
                    });
                }

                var created = await _repo.CreateSetupAsync(request);
                return Ok(new
                {
                    message = "New setupId created",
                    SetUpId = created.SetUpID,
                    setup = created
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking or creating setup.");
                if (ex.Message == "Invalid Operator ID")
                    return BadRequest(new { message = ex.Message });

                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpPost("start-setup")]
        public async Task<IActionResult> StartSetup([FromBody] SetupIdentifierRequest request)
        {
            try
            {
                var result = await _repo.StartSetupAsync(request.SetUpID);

                return result == "Setup started"
                    ? Ok(new { message = result })
                    : BadRequest(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while starting setup.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpPost("toggle-pause")]
        public async Task<IActionResult> TogglePause([FromBody] SetupPauseRequest request)
        {
            try
            {
                var result = await _repo.TogglePauseAsync(request);

                return (result == "Setup paused" || result == "Setup resumed")
                    ? Ok(new { message = result })
                    : BadRequest(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling pause.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpPost("end-setup")]
        public async Task<IActionResult> EndOperatorTime([FromBody] SetupIdentifierRequest request)
        {
            try
            {
                var success = await _repo.EndSetupTimeAsync(request.SetUpID);

                return success
                    ? Ok(new { message = "Operator end time updated successfully." })
                    : NotFound(new { message = "Setup not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while ending setup time.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpPost("add-delays")]
        public async Task<IActionResult> AddDelays([FromBody] SetupDelayRequest request)
        {
            try
            {
                var result = await _repo.InsertDelaysAsync(request);

                return result
                    ? Ok(new { message = "Delays added successfully" })
                    : BadRequest(new { message = "Failed to add delays" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding delays.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }
    }
}
