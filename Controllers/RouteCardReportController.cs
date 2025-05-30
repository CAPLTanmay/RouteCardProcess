using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.RouteCardReport;
using Microsoft.Extensions.Logging;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RouteCardReportController : ControllerBase
    {
        private readonly IRouteCardReportRepository _repo;
        private readonly ILogger<RouteCardReportController> _logger;

        public RouteCardReportController(IRouteCardReportRepository repo, ILogger<RouteCardReportController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpPost("get-by-workorder")]
        public async Task<IActionResult> GetRouteCardReport([FromBody] WorkOrderRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.WorkOrderNo))
                    return BadRequest(new { message = "WorkOrderNo is required." });

                var result = await _repo.GetRouteCardReportAsync(request.WorkOrderNo);
                if (result == null || !result.Any())
                    return NotFound(new { message = "No data found for the provided WorkOrderNo." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching route card report.");
                return StatusCode(500, new { message = "An unexpected error occurred.", error = ex.Message });
            }
        }
    }
}
