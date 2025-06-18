using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Department;
using RouteCardProcess.Repositories;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentRepository _repo;
    private readonly ISystemLoggerRepository _systemLogger;
    private readonly IUserMessageService _userMessageService;

    public DepartmentController(IDepartmentRepository repo, ISystemLoggerRepository systemLogger, IUserMessageService userMessageService)
    {
        _repo = repo;
        _systemLogger = systemLogger;
        _userMessageService = userMessageService;
    }

    [HttpGet("GetAllDepartments")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var departments = await _repo.GetAllAsync();
            return Ok(departments);
        }
        catch (Exception ex)
        {
            await _systemLogger.LogAsync("DepartmentController", "GetAllDepartments", ex.ToString());
            return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(5001) });
        }
    }

    [HttpPost("CreateDepartment")]
    public async Task<IActionResult> Create([FromBody] DepartmentMasterDto department)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _repo.AddAsync(department);
            if (result > 0)
                return Ok(new { success = true, message = _userMessageService.GetMessage(5003) });
            else
                return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(5004) });
        }
        catch (Exception ex)
        {
            await _systemLogger.LogAsync("DepartmentController", "CreateDepartment", ex.ToString());
            return StatusCode(500, new { success = false, message = _userMessageService.GetMessage(5001) });
        }
    }
}
