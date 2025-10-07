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
    public class IdleCodeController : ControllerBase
    {
        private readonly IIdleCodeRepository _idleCodeRepository;
        private readonly IUserMessageService _userMessageService;

        public IdleCodeController(IIdleCodeRepository idleCodeRepository, IUserMessageService userMessageService)
        {
            _idleCodeRepository = idleCodeRepository;
            _userMessageService = userMessageService;
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("addIdleCode")]
        public async Task<IActionResult> AddIdleCode([FromBody] IdleCodeRequest request)
        {
            try
            {
                var rowsAffected = await _idleCodeRepository.AddIdleCodeAsync(request);
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
        [HttpPost("updateIdleCode")]
        public async Task<IActionResult> UpdateIdleCode([FromBody] IdleCodeRequest request)
        {
            try
            {
                var rowsAffected = await _idleCodeRepository.UpdateIdleCodeAsync(request);
                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) })  
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("allIdleCode")]
        public async Task<IActionResult> GetAllIdleCodes()
        {
            try
            {
                var idleCodes = await _idleCodeRepository.GetAllIdleCodesAsync();
                return Ok(idleCodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("deleteIdleCode")]
        public async Task<IActionResult> DeleteIdleCode([FromBody] DeleteCodeRequest request)
        {
            try
            {
                var rowsAffected = await _idleCodeRepository.DeleteIdleCodeAsync(request.Plant, request.IdleCode);
                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) }) // success
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) }); // failure
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

    }
}
