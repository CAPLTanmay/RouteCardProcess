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
    public class StdExceptionController : ControllerBase
    {
        private readonly IStdExceptionRepository _stdExceptionRepository;
        private readonly IUserMessageService _userMessageService;

        public StdExceptionController(IStdExceptionRepository stdExceptionRepository, IUserMessageService userMessageService)
        {
            _stdExceptionRepository = stdExceptionRepository;
            _userMessageService = userMessageService;
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("addStdException")]
        public async Task<IActionResult> AddStdException([FromBody] StdExceptionRequest request)
        {
            try
            {
                var result = await _stdExceptionRepository.AddStdExceptionAsync(request);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(5003) }) // Inserted successfully
                    : BadRequest(new { message = _userMessageService.GetMessage(5004) }); // Insert failed
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("updateStdException")]
        public async Task<IActionResult> UpdateStdException([FromBody] StdExceptionRequest request)
        {
            try
            {
                var result = await _stdExceptionRepository.UpdateStdExceptionAsync(request);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) }) // Updated successfully
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) }); // Update failed or not found
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("allStdExceptions")]
        public async Task<IActionResult> GetAllStdExceptions()
        {
            try
            {
                var result = await _stdExceptionRepository.GetAllStdExceptionsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("deleteStdException")]
        public async Task<IActionResult> DeleteStdException([FromBody] DeleteStdExceptionRequest request)
        {
            try
            {
                var result = await _stdExceptionRepository.DeleteStdExceptionAsync(request.Reason_Code);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) }) // Deactivated successfully
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) }); // Not found
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }
    }
}
