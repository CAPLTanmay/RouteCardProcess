using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PauseCodeController : ControllerBase
    {
        private readonly IPauseCodeRepository _pauseCodeRepository;
        private readonly IUserMessageService _userMessageService;

        public PauseCodeController(IPauseCodeRepository pauseCodeRepository, IUserMessageService userMessageService)
        {
            _pauseCodeRepository = pauseCodeRepository;
            _userMessageService = userMessageService;
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("addPauseCode")]
        public async Task<IActionResult> AddPauseCode([FromBody] PauseCodeRequest request)
        {
            try
            {
                var rowsAffected = await _pauseCodeRepository.AddPauseCodeAsync(request);
                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(5003) })
                    : BadRequest(new { message = _userMessageService.GetMessage(5004) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("updatePauseCode")]
        public async Task<IActionResult> UpdatePauseCode([FromBody] PauseCodeRequest request)
        {
            try
            {
                var rowsAffected = await _pauseCodeRepository.UpdatePauseCodeAsync(request);
                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("allPauseCode")]
        public async Task<IActionResult> GetAllPauseCodes()
        {
            try
            {
                var pauseCodes = await _pauseCodeRepository.GetAllPauseCodesAsync();
                return Ok(pauseCodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("deletePauseCode")]
        public async Task<IActionResult> DeletePauseCode([FromBody] DeletePauseCodeRequest request)
        {
            try
            {
                var rowsAffected = await _pauseCodeRepository.DeletePauseCodeAsync(request.Plant, request.PauseCode);
                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

    }
}
