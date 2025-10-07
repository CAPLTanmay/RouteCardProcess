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
        var blacklistService = context.RequestServices.GetRequiredService<ITokenBlacklistService>();

        var token = context.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(token))
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            if (await blacklistService.IsTokenRevokedAsync(jti))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token revoked or expired. Please login again.");
                return;
            }
        }

        await _next(context);
    }
}
