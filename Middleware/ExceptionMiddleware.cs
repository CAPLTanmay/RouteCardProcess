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
                    // Use scoped dependency for DB logger
                    using var scope = _serviceProvider.CreateScope();
                    var logger = scope.ServiceProvider.GetRequiredService<ISystemLoggerRepository>();

                    await logger.LogAsync(
                        "Global",
                        "ExceptionMiddleware",
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}");
                }
                catch
                {
                    // Avoid throwing from logging
                }

                // Return safe response
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = new
                {
                    status = 500,
                    message = _env.IsDevelopment()
                        ? $"Internal Server Error (Developer Mode): {ex.Message}"
                        : "An unexpected error occurred. Please contact your administrator.",
                    // In production, never include stack trace
                    details = _env.IsDevelopment() ? ex.StackTrace : null
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
            }
        }
    }
}
