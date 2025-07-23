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
    public class OrderTypeController : ControllerBase
    {
        private readonly IOrderTypeRepository _orderTypeRepository;
        private readonly IUserMessageService _userMessageService;

        public OrderTypeController(IOrderTypeRepository orderTypeRepository, IUserMessageService userMessageService)
        {
            _orderTypeRepository = orderTypeRepository;
            _userMessageService = userMessageService;
        }

        [HttpPost("addOrderType")]
        public async Task<IActionResult> AddOrderType([FromBody] OrderTypeRequest request)
        {
            try
            {
                var rowsAffected = await _orderTypeRepository.AddOrderTypeAsync(request);
                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(5003) })
                    : BadRequest(new { message = _userMessageService.GetMessage(5004) });
            }
            catch (ApplicationException ex) when (ex.Message == "OrderType already exists")
            {
                return Conflict(new { message = _userMessageService.GetMessage(1105) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("updateOrderType")]
        public async Task<IActionResult> UpdateOrderType([FromBody] OrderTypeRequest request)
        {
            try
            {
                var rowsAffected = await _orderTypeRepository.UpdateOrderTypeAsync(request);
                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1096) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("allOrderTypes")]
        public async Task<IActionResult> GetAllOrderTypes()
        {
            try
            {
                var orderTypes = await _orderTypeRepository.GetAllOrderTypesAsync();
                return Ok(orderTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("deleteOrderType")]
        public async Task<IActionResult> DeleteOrderType([FromBody] DeleteOrderTypeRequest request)
        {
            try
            {
                var rowsAffected = await _orderTypeRepository.DeleteOrderTypeAsync(request.Plant, request.OrderType);
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
