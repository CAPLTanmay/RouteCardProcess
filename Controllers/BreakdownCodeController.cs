using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BreakdownCodeController : ControllerBase
    {
        private readonly IBreakdownCodeRepository _repository;
        private readonly IUserMessageService _userMessageService;

        public BreakdownCodeController(IBreakdownCodeRepository repository, IUserMessageService userMessageService)
        {
            _repository = repository;
            _userMessageService = userMessageService;
        }

        [HttpPost("addBreakdownCode")]
        public async Task<IActionResult> Add([FromBody] BreakdownCodeRequest request)
        {
            try
            {
                var result = await _repository.AddAsync(request);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(5003) })
                    : BadRequest(new { message = _userMessageService.GetMessage(5004) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("updateBreakdownCode")]
        public async Task<IActionResult> Update([FromBody] BreakdownCodeRequest request)
        {
            try
            {
                var result = await _repository.UpdateAsync(request);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("allBreakdownCode")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _repository.GetAllAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("deleteBreakdownCode")]
        public async Task<IActionResult> Delete([FromBody] BreakdownCodeRequest request)
        {
            try
            {
                var result = await _repository.DeleteAsync(request.BreakdownCodeGroup, request.BreakdownCode);
                return result > 0
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
