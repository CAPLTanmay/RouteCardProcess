using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Model;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MachiningController : ControllerBase
    {
        private readonly MachiningRepository _repo;

        public MachiningController(MachiningRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("check-or-create")]
        public async Task<IActionResult> CheckOrCreateMachining([FromBody] MachiningDto request)
        {
            // You could add logic here to check for an existing Machining if needed
            var existing = await _repo.GetByCompositeKeyAsync(request.WorkCenterNo, request.WorkOrderNo, request.OperationNo);

            if (existing != null)
            {
                return Ok(new { message = "Machining already exists", machiningID = existing.MachiningID, machining = existing });
            }

            try
            {
                var created = await _repo.CreateMachiningAsync(request);
                return Ok(new { message = "New machining created", machiningID = created.MachiningID, machining = created });
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid Operator ID")
                {
                    return BadRequest(new { message = ex.Message });
                }

                // Optional: Log the full exception
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }

        }

        [HttpPost("start-machining")]
        public async Task<IActionResult> StartMachining([FromBody] MachiningIdentifierRequest request)
        {
            var result = await _repo.StartMachiningAsync(request.MachiningID);

            if (result == "Machining started")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }

        [HttpPost("toggle-pause")]
        public async Task<IActionResult> TogglePause([FromBody] MachiningPauseRequest request)
        {
            var result = await _repo.TogglePauseAsync(request);

            if (result == "Machining paused" || result == "Machining resumed")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }

        [HttpPost("end-machining")]
        public async Task<IActionResult> EndOperatorTime([FromBody] MachiningIdentifierRequest request)
        {
            var success = await _repo.EndMachiningTimeAsync(request.MachiningID);

            if (success)
                return Ok(new { message = "Operator end time updated successfully." });

            return NotFound(new { message = "Machining record not found." });
        }

        [HttpPost("add-delays")]
        public async Task<IActionResult> AddDelays([FromBody] MachiningDelayRequest request)
        {
            var result = await _repo.InsertDelaysAsync(request);
            return result ? Ok(new { message = "Delays added successfully" }) : BadRequest(new { message = "Failed to add delays" });
        }
    }
}
