using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExceptionReasonController : ControllerBase
    {
        private readonly IExceptionReasonRepository _exceptionReasonRepository;
        private readonly IUserMessageService _userMessageService;

        public ExceptionReasonController(IExceptionReasonRepository exceptionReasonRepository, IUserMessageService userMessageService)
        {
            _exceptionReasonRepository = exceptionReasonRepository;
            _userMessageService = userMessageService;
        }

        [HttpPost("addExceptionReason")]
        public async Task<IActionResult> AddExceptionReason([FromBody] ExceptionReasonRequest request)
        {
            try
            {
                int result = await _exceptionReasonRepository.AddExceptionReasonAsync(request);

                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(5003) }) 
                    : BadRequest(new { message = _userMessageService.GetMessage(5004) }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("updateExceptionReason")]
        public async Task<IActionResult> UpdateExceptionReason([FromBody] ExceptionReasonRequest request)
        {
            try
            {
                int result = await _exceptionReasonRepository.UpdateExceptionReasonAsync(request);

                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) }) 
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("allExceptionReasons")]
        public async Task<IActionResult> GetAllExceptionReasons()
        {
            try
            {
                var result = await _exceptionReasonRepository.GetAllExceptionReasonsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }
    }
}
