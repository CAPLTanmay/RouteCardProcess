using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Model;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddHelperController : ControllerBase
    {
        private readonly HelperRepository _helperRepository;

        public AddHelperController(HelperRepository helperRepository)
        {
            _helperRepository = helperRepository;
        }

        [HttpPost("add-helper")]
        public async Task<IActionResult> AddHelper([FromBody] HelperRequest request)
        {
            var result = await _helperRepository.AddHelperAsync(request);

            if (result == "Helper added successfully.")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }

        // Endpoint for ending a helper
        [HttpPost("end-helper")]
        public async Task<IActionResult> EndHelper([FromBody] EndHelperRequest request)
        {
            var result = await _helperRepository.EndHelperAsync(request);

            if (result == "Helper end time updated successfully.")
                return Ok(new { message = result });

            return BadRequest(new { message = result });
        }

        [HttpPost("toggle-helper-pause")]
        public async Task<IActionResult> ToggleHelperPause([FromBody] EndHelperRequest request)
        {
            var result = await _helperRepository.ToggleHelperPauseAsync(request);
            return Ok(new { message = result });
        }

    }
}
