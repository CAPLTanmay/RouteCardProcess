using System.Net;
using System.Text.Json;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, IServiceProvider serviceProvider, IHostEnvironment env)
        {
            _next = next;
            _serviceProvider = serviceProvider;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                try
                {
                    // Create scope and resolve the logger
                    using var scope = _serviceProvider.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ISystemLoggerRepository>();
                    await logger.LogAsync("ExceptionMiddleware", "Invoke", ex.ToString());
                }
                catch
                {
                    // Log failure in logger silently or to a file, if needed
                }

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = new
                {
                    message = _env.IsDevelopment() ? ex.Message : "Internal Server Error",
                    stackTrace = _env.IsDevelopment() ? ex.StackTrace : null
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(json);
            }
        }
    }
}
