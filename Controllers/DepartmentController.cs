using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Model;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentRepository _repo;

    public DepartmentController(IDepartmentRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("GetAllDepartments")]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _repo.GetAllAsync();
        return Ok(departments);
    }

    [HttpPost("CreateDepartment")]
    public async Task<IActionResult> Create([FromBody] DepartmentMaster department)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _repo.AddAsync(department);
        if (result > 0)
            return Ok(new { success = true, message = "Inserted successfully" });
        else
            return StatusCode(500, new { success = false, message = "Insert failed" });
    }
}
