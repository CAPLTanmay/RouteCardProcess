using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Helper;
using Microsoft.Extensions.Logging;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddHelperController : ControllerBase
    {
        private readonly IHelperRepository _helperRepository;
        private readonly ILogger<AddHelperController> _logger;

        public AddHelperController(IHelperRepository helperRepository, ILogger<AddHelperController> logger)
        {
            _helperRepository = helperRepository;
            _logger = logger;
        }

        [HttpPost("add-helper")]
        public async Task<IActionResult> AddHelper([FromBody] HelperRequest request)
        {
            try
            {
                var result = await _helperRepository.AddHelperAsync(request);

                if (result == "Helper added successfully.")
                    return Ok(new { message = result });

                return BadRequest(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddHelper");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("end-helper")]
        public async Task<IActionResult> EndHelper([FromBody] EndHelperRequest request)
        {
            try
            {
                var result = await _helperRepository.EndHelperAsync(request);

                if (result == "Helper end time updated and released successfully.")
                    return Ok(new { message = result });

                return BadRequest(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EndHelper");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("toggle-helper-pause")]
        public async Task<IActionResult> ToggleHelperPause([FromBody] EndHelperRequest request)
        {
            try
            {
                var result = await _helperRepository.ToggleHelperPauseAsync(request);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ToggleHelperPause");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("helpers")]
        public async Task<IActionResult> GetHelpersByMainOperatorId([FromBody] MainOperatorRequestDto request)
        {
            try
            {
                var helpers = await _helperRepository.GetHelpersByMainOperatorIdAsync(request.MainOperatorId);
                return Ok(helpers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHelpersByMainOperatorId");
                return StatusCode(500, "Internal server error.");
            }
        }

    }
}
