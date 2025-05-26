using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BreakDownController : ControllerBase
    {
        private readonly IBreakDownRepository _repo;

        public BreakDownController(IBreakDownRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] BreakDownStartRequest request)
        {
            var success = await _repo.StartBreakDownAsync(request.WorkCenterNo, request.OperatorId, request.BreakDownReasonCode);
            if (success)
                return Ok(new { message = "Breakdown started successfully" });
            else
                return BadRequest(new { message = "Failed to start breakdown" });
        }

        [HttpPost("end")]
        public async Task<IActionResult> End([FromBody] BreakDownEndRequest request)
        {
            var success = await _repo.EndBreakDownAsync(request.WorkCenterNo, request.OperatorId, request.BreakDownReasonCode);
            if (success)
                return Ok(new { message = "Breakdown ended successfully" });
            else
                return NotFound(new { message = "No open breakdown found for this work center" });
        }

    }
}
