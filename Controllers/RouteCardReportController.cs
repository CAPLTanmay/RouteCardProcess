using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RouteCardReportController : ControllerBase
    {
        private readonly RouteCardReportRepository _repository;

        public RouteCardReportController(RouteCardReportRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("get-by-workorder")]
        public async Task<IActionResult> GetRouteCardReport([FromBody] WorkOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WorkOrderNo))
                return BadRequest(new { message = "WorkOrderNo is required." });

            var result = await _repository.GetRouteCardReportAsync(request.WorkOrderNo);
            if (result == null || !result.Any())
                return NotFound(new { message = "No data found for the provided WorkOrderNo." });

            return Ok(result);
        }
    }
}
