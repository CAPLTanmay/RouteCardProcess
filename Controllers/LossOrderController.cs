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
    public class LossOrderController : ControllerBase
    {
        private readonly ILossOrderRepository _lossOrderRepository;
        private readonly IUserMessageService _userMessageService;

        public LossOrderController(ILossOrderRepository lossOrderRepository, IUserMessageService userMessageService)
        {
            _lossOrderRepository = lossOrderRepository;
            _userMessageService = userMessageService;
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("addLossOrder")]
        public async Task<IActionResult> AddLossOrder([FromBody] LossOrderRequest request)
        {
            try
            {
                var result = await _lossOrderRepository.AddLossOrderAsync(request);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(5003) })
                    : BadRequest(new { message = _userMessageService.GetMessage(5004) });
            }
            catch (ApplicationException ex) when (ex.Message == "LossOrder already exists.")
            {
                return Conflict(new { message = _userMessageService.GetMessage(1106) }); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("updateLossOrder")]
        public async Task<IActionResult> UpdateLossOrder([FromBody] DeleteLossOrderRequest request)
        {
            try
            {
                var result = await _lossOrderRepository.UpdateLossOrderAsync(request);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("allLossOrders")]
        public async Task<IActionResult> GetAllLossOrders()
        {
            try
            {
                var data = await _lossOrderRepository.GetAllLossOrdersAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [Authorize(Roles = "Supervisor,DRC_admin")]
        [HttpPost("deleteLossOrder")]
        public async Task<IActionResult> DeleteLossOrder([FromBody] DeleteLossOrderRequest request)
        {
            try
            {
                var result = await _lossOrderRepository.DeleteLossOrderAsync(request);

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
