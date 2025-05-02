using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Model;
using RouteCardProcess.Repositories;

namespace MyApiProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly DepartmentRepository _repo;

        public DepartmentController(DepartmentRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _repo.GetAllAsync();
            return Ok(departments);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepartmentMaster department)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _repo.AddAsync(department);
            return result > 0 ? Ok("Inserted successfully") : StatusCode(500, "Insert failed");
        }
    }
}
