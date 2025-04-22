using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Model;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SetUpTransController : ControllerBase
    {
        private readonly SetUpTransRepository _repo;

        public SetUpTransController(SetUpTransRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("check-or-create")]
        public async Task<IActionResult> CheckOrCreateSetup([FromBody] SetupMaster request)
        {
            var existing = await _repo.GetByCompositeKeyAsync(request.WorkCenterNo, request.WorkOrderNo, request.OperationNo);

            if (existing != null)
            {
                return Ok(new { message = "Setup already exists", setUpID = existing.SetUpID, setup = existing });
            }

            var created = await _repo.CreateSetupAsync(request);
            return Ok(new { message = "New setupId created",setUpId=created.SetUpID, setup = created });
        }

        [HttpPost("start-setup")]
        public async Task<IActionResult> StartSetup([FromBody] SetupIdentifierRequest request)
        {
            var result = await _repo.StartSetupAsync(request.SetUpID);

            if (result == "Setup started")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }

        [HttpPost("toggle-pause")]
        public async Task<IActionResult> TogglePause([FromBody] SetupPauseRequest request)
        {
            var result = await _repo.TogglePauseAsync(request);

            if (result == "Setup paused" || result == "Setup resumed")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }

        [HttpPost("end-setup")]
        public async Task<IActionResult> EndOperatorTime([FromBody] SetupIdentifierRequest request)
        {
            var success = await _repo.EndSetupTimeAsync(request.SetUpID);

            if (success)
                return Ok(new { message = "Operator end time updated successfully." });

            return NotFound(new { message = "Setup not found." });
        }

        [HttpPost("add-delays")]
        public async Task<IActionResult> AddDelays([FromBody] SetupDelayRequest request)
        {
            var result = await _repo.InsertDelaysAsync(request);
            return result ? Ok(new { message = "Delays added successfully" }) : BadRequest(new { message = "Failed to add delays" });
        }

    }
}
