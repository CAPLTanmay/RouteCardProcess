using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StdExceptionController : ControllerBase
    {
        private readonly IStdExceptionRepository _stdExceptionRepository;
        private readonly IUserMessageService _userMessageService;

        public StdExceptionController(IStdExceptionRepository stdExceptionRepository, IUserMessageService userMessageService)
        {
            _stdExceptionRepository = stdExceptionRepository;
            _userMessageService = userMessageService;
        }

        [HttpPost("addStdException")]
        public async Task<IActionResult> AddStdException([FromBody] StdExceptionRequest request)
        {
            try
            {
                var result = await _stdExceptionRepository.AddStdExceptionAsync(request);
                if (result > 0)
                    return Ok(new { message = _userMessageService.GetMessage(5003) });
                else
                    return BadRequest(new { message = _userMessageService.GetMessage(5004) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("updateStdException")]
        public async Task<IActionResult> UpdateStdException([FromBody] StdExceptionRequest request)
        {
            try
            {
                var result = await _stdExceptionRepository.UpdateStdExceptionAsync(request);
                if (result > 0)
                    return Ok(new { message = _userMessageService.GetMessage(1095) });
                else
                    return BadRequest(new { message = _userMessageService.GetMessage(1096) });
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
    }
}
