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
    public class MstWorkCenterController : ControllerBase
    {
        private readonly IMstWorkCenterRepository _mstWorkCenterRepository;
        private readonly IUserMessageService _userMessageService;

        public MstWorkCenterController(IMstWorkCenterRepository mstWorkCenterRepository, IUserMessageService userMessageService)
        {
            _mstWorkCenterRepository = mstWorkCenterRepository;
            _userMessageService = userMessageService;
        }

        [HttpPost("addMstWorkCenter")]
        public async Task<IActionResult> AddMstWorkCenter([FromBody] MstWorkCenterRequest request)
        {
            try
            {
                var rowsAffected = await _mstWorkCenterRepository.AddMstWorkCenterAsync(request);

                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1091) })
                    : BadRequest(new { message = _userMessageService.GetMessage(1092) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("updateMstWorkCenter")]
        public async Task<IActionResult> UpdateMstWorkCenter([FromBody] MstWorkCenterRequest request)
        {
            try
            {
                var rowsAffected = await _mstWorkCenterRepository.UpdateMstWorkCenterAsync(request);

                return rowsAffected > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1093) })
                    : NotFound(new { message = _userMessageService.GetMessage(1094) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("allMstWorkCenter")]
        public async Task<IActionResult> GetAllMstWorkCenters()
        {
            try
            {
                var result = await _mstWorkCenterRepository.GetAllMstWorkCentersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var result = await _mstWorkCenterRepository.GetDistinctDepartmentsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("workcenters")]
        public async Task<IActionResult> GetWorkCentersByDept([FromBody] DeptRequest request)
        {
            if (string.IsNullOrEmpty(request?.Dept))
                return BadRequest("Dept is required");

            try
            {
                var result = await _mstWorkCenterRepository.GetWorkCentersByDeptAsync(request.Dept);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }

        [HttpPost("deleteMstWorkCenter")]
        public async Task<IActionResult> DeleteMstWorkCenter([FromBody] MstWorkCenterDeleteRequest request)
        {
            if (string.IsNullOrEmpty(request?.Plant) || string.IsNullOrEmpty(request?.WorkCenter))
                return BadRequest(new { message = "Plant and WorkCenter are required" });

            try
            {
                var result = await _mstWorkCenterRepository.DeleteMstWorkCenterAsync(request.Plant, request.WorkCenter);
                return result > 0
                    ? Ok(new { message = _userMessageService.GetMessage(1095) }) // "Deleted successfully"
                    : NotFound(new { message = _userMessageService.GetMessage(1096) }); // "WorkCenter not found"
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = _userMessageService.GetMessage(5001), error = ex.Message });
            }
        }
    }
}
