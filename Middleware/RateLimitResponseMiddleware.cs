using System.Text.Json;
using RouteCardProcess.Interfaces;

namespace RouteCardProcess.Middleware
{
    public class RateLimitResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

        public RateLimitResponseMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == 503)
            {
                context.Response.Clear();
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";

                var ip = context.Connection.RemoteIpAddress?.ToString();
                var response = new
                {
                    status = 429,
                    message = "Too many login attempts. Please try again after some time.",
                    clientIp = ip,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);

                //  Create a new scope so we can resolve scoped services safely
                using (var scope = _scopeFactory.CreateScope())
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ISystemLoggerRepository>();
                    await logger.LogAsync("RateLimiter", "LoginRateLimit",
                        $"Too many login attempts from {ip} at {DateTime.Now}");
                }
            }
        }
    }
}
