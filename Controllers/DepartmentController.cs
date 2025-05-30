using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model.DTOs.Department;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentRepository _repo;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(IDepartmentRepository repo, ILogger<DepartmentController> logger)
    {
        _repo = repo;
        _logger = logger;
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
            _logger.LogError(ex, "Error in GetAllDepartments");
            return StatusCode(500, new { success = false, message = "Internal server error." });
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
                return Ok(new { success = true, message = "Inserted successfully" });
            else
                return StatusCode(500, new { success = false, message = "Insert failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateDepartment");
            return StatusCode(500, new { success = false, message = "Internal server error." });
        }
    }
}
