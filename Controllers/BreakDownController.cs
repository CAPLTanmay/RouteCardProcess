using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.BreakDownDto;
using Microsoft.Extensions.Logging;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BreakDownController : ControllerBase
    {
        private readonly IBreakDownRepository _repo;
        private readonly ILogger<BreakDownController> _logger;

        public BreakDownController(IBreakDownRepository repo, ILogger<BreakDownController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] BreakDownStartRequest request)
        {
            try
            {
                var success = await _repo.StartBreakDownAsync(request.WorkCenterNo, request.OperatorId, request.BreakDownReasonCode);
                if (success)
                    return Ok(new { message = "Breakdown started successfully" });
                else
                    return BadRequest(new { message = "Failed to start breakdown" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Start breakdown");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        [HttpPost("end")]
        public async Task<IActionResult> End([FromBody] BreakDownEndRequest request)
        {
            try
            {
                var success = await _repo.EndBreakDownAsync(request.WorkCenterNo, request.OperatorId, request.BreakDownReasonCode);
                if (success)
                    return Ok(new { message = "Breakdown ended successfully" });
                else
                    return NotFound(new { message = "No open breakdown found for this work center" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in End breakdown");
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
    }
}
