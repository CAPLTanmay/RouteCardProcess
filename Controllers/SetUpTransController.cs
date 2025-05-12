using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Model;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SetUpTransController : ControllerBase
    {
        private readonly SetUpTransRepository _repo;

        public SetUpTransController(SetUpTransRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("check-status")]
        public async Task<IActionResult> CheckStatus([FromBody] SetupMasterDto request)
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



        [HttpPost("check-or-create")]
        public async Task<IActionResult> CheckOrCreateSetup([FromBody] SetupMasterDto request)
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

            try
            {
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
                if (ex.Message == "Invalid Operator ID")
                    return BadRequest(new { message = ex.Message });

                return StatusCode(500, new
                {
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("start-setup")]
        public async Task<IActionResult> StartSetup([FromBody] SetupIdentifierRequest request)
        {
            var result = await _repo.StartSetupAsync(request.SetUpID);

            return result == "Setup started"
                ? Ok(new { message = result })
                : BadRequest(new { message = result });
        }

        [HttpPost("toggle-pause")]
        public async Task<IActionResult> TogglePause([FromBody] SetupPauseRequest request)
        {
            var result = await _repo.TogglePauseAsync(request);

            return (result == "Setup paused" || result == "Setup resumed")
                ? Ok(new { message = result })
                : BadRequest(new { message = result });
        }

        [HttpPost("end-setup")]
        public async Task<IActionResult> EndOperatorTime([FromBody] SetupIdentifierRequest request)
        {
            var success = await _repo.EndSetupTimeAsync(request.SetUpID);

            return success
                ? Ok(new { message = "Operator end time updated successfully." })
                : NotFound(new { message = "Setup not found." });
        }

        [HttpPost("add-delays")]
        public async Task<IActionResult> AddDelays([FromBody] SetupDelayRequest request)
        {
            var result = await _repo.InsertDelaysAsync(request);

            return result
                ? Ok(new { message = "Delays added successfully" })
                : BadRequest(new { message = "Failed to add delays" });
        }
    }
}
