using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Model;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddHelperController : ControllerBase
    {
        private readonly IHelperRepository _helperRepository;

        public AddHelperController(IHelperRepository helperRepository)
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

        [HttpGet("helpers/{mainOperatorId}")]
        public async Task<IActionResult> GetHelpersByMainOperatorId(string mainOperatorId)
        {
            var helpers = await _helperRepository.GetHelpersByMainOperatorIdAsync(mainOperatorId);
            return Ok(helpers);
        }
    }
}
