using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RouteCardReportController : ControllerBase
    {
        private readonly IRouteCardReportRepository _repo;

        public RouteCardReportController(IRouteCardReportRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("get-by-workorder")]
        public async Task<IActionResult> GetRouteCardReport([FromBody] WorkOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WorkOrderNo))
                return BadRequest(new { message = "WorkOrderNo is required." });

            var result = await _repo.GetRouteCardReportAsync(request.WorkOrderNo);
            if (result == null || !result.Any())
                return NotFound(new { message = "No data found for the provided WorkOrderNo." });

            return Ok(result);
        }
    }
}
