using RouteCardProcess.Interfaces;
using System.IdentityModel.Tokens.Jwt;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public JwtBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // 1) If Skip flag set by SkipJwtForAnonymousMiddleware, bypass blacklist
        if (context.Items.ContainsKey("SkipJwtAuth"))
        {
            await _next(context);
            return;
        }

        // 2) Also skip swagger/static/health optionally (defense in depth)
        var path = (context.Request.Path.Value ?? "").ToLowerInvariant();
        if (path.StartsWith("/swagger") || path.StartsWith("/favicon") || path.StartsWith("/health"))
        {
            await _next(context);
            return;
        }

        var blacklistService = context.RequestServices.GetRequiredService<ITokenBlacklistService>();

        // 3) If user is not authenticated, return 401 (this middleware only runs for protected routes)
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Missing or invalid token.");
            return;
        }

        // 4) Read jti from claims if available
        var jti = context.User?.Claims?.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

        // 5) If not present, try to read from Authorization header as fallback
        if (string.IsNullOrEmpty(jti))
        {
            var tokenHeader = context.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "").Trim();
            if (!string.IsNullOrEmpty(tokenHeader))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(tokenHeader);
                    jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized: Invalid token.");
                    return;
                }
            }
        }

        // 6) Check blacklist if we have a jti
        if (!string.IsNullOrEmpty(jti) && await blacklistService.IsTokenRevokedAsync(jti))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token revoked or expired. Please login again.");
            return;
        }

        await _next(context);
    }
}
