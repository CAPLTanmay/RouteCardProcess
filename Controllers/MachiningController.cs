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
        public async Task<IActionResult> CheckOrCreateMachining([FromBody] MachiningMaster request)
        {
            var existing = await _repo.GetByCompositeKeyAsync(request.WorkCenterNo, request.WorkOrderNo, request.OperationNo);

            if (existing != null)
            {
                return Ok(new { message = "Machining already exists", machiningId = existing.MachiningID, machining = existing });
            }

            var created = await _repo.CreateMachiningAsync(request);
            return Ok(new { message = "New machiningId created", machiningId = created.MachiningID, machining = created });
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
