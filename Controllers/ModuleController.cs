using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModuleController : ControllerBase
    {
        private readonly IModuleRepository _moduleRepository;
        private readonly IConfiguration _config;

        public ModuleController(IModuleRepository moduleRepository, IConfiguration config)
        {
            _moduleRepository = moduleRepository;
            _config = config;
        }
        [AllowAnonymous]
        [HttpGet("GetAllModules")]
        public async Task<IActionResult> GetAllModules()
        {
            string apiKey = Request.Headers["X-API-KEY"].FirstOrDefault();
            string validKey = _config.GetValue<string>("ApiSettings:XApiKey");

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey != validKey)
            {
                return Unauthorized(new
                {
                    result = (object)null,
                    message = "Unauthorized",
                    statusCode = 401,
                    errors = new[] { "Invalid or missing API key." }
                });
            }

            var modules = await _moduleRepository.GetAllModulesAsync();

            return Ok(new
            {
                result = modules,
                message = "Success",
                statusCode = 200,
                errors = new string[] { }
            });
        }
    }
}
