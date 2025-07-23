using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.Entities;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BreakdownGroupCodeController : ControllerBase
    {
        private readonly IBreakdownGroupCodeRepository _repository;
        private readonly IUserMessageService _userMessageService;

        public BreakdownGroupCodeController(IBreakdownGroupCodeRepository repository, IUserMessageService userMessageService)
        {
            _repository = repository;
            _userMessageService = userMessageService;
        }

        [HttpPost("addBreakdownGroupCode")]
        public async Task<IActionResult> Add([FromBody] BreakdownGroupCodeRequest request)
        {
            try
            {
                var result = await _repository.AddAsync(request);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(5003) })
                    : BadRequest(new { message = _userMessageService.GetMessage(5004) });
            }
            catch (SqlException ex) when (ex.Message.Contains(_userMessageService.GetMessage(1107)))
            {
                return Conflict(new { message = _userMessageService.GetMessage(1107) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("updateBreakdownGroupCode")]
        public async Task<IActionResult> Update([FromBody] BreakdownGroupCodeRequest request)
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

        [HttpGet("getAllBreakdownGroupCode")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _repository.GetAllAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("deleteBreakdownGroupCode")]
        public async Task<IActionResult> Delete([FromBody] string breakdownCodeGroup)
        {
            try
            {
                var result = await _repository.DeleteAsync(breakdownCodeGroup);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("getBreakdownCode")]
        public async Task<IActionResult> GetByGroup([FromBody] BreakdownCodesByGroup request)
        {
            try
            {
                var result = await _repository.GetByGroupAsync(request.BreakdownCodeGroup);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

    }

}
