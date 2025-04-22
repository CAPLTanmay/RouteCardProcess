using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Model;
using RouteCardProcess.Repositories;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogInController : ControllerBase
    {
        private readonly LogInRepository _repo;

        public LogInController(LogInRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var logins = await _repo.GetAllAsync();
            return Ok(logins);
        }

        [HttpPost]
        public async Task<IActionResult> Create(LogInMaster login)
        {
            var result = await _repo.AddAsync(login);
            return result > 0 ? Ok("User created") : StatusCode(500, "Insert failed");
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateLogin([FromBody] LoginRequest request)
        {
            var user = await _repo.ValidateLoginAsync(request.OperatorId, request.Password);

            if (user != null)
            {
                return Ok(new { message = "Login successful", user });
            }
            else
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] SetupIdentifierRequest request)
        {
            var result = await _repo.TryLogoutAsync(request.SetUpID);
            return result == "OK"
                ? Ok(new { message = "Logout successful" })
                : BadRequest(new { message = result });
        }


    }

}
