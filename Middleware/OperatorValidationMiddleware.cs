using System.Security.Claims;
using System.Text.Json;
using RouteCardProcess.Interfaces;

public class OperatorValidationMiddleware
{
    private readonly RequestDelegate _next;

    public OperatorValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // resolve logger per request scope
        var systemLogger = context.RequestServices.GetService<ISystemLoggerRepository>();

        try
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var tokenOperatorId = context.User.FindFirst(ClaimTypes.Name)?.Value;

                if ((context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put) &&
                    context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(body);
                            if (jsonDoc.RootElement.TryGetProperty("operatorId", out var operatorIdProp))
                            {
                                var requestOperatorId = operatorIdProp.GetString();
                                if (!string.IsNullOrEmpty(requestOperatorId) &&
                                    requestOperatorId != tokenOperatorId)
                                {
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    await context.Response.WriteAsJsonAsync(new
                                    {
                                        success = false,
                                        message = "Token does not belong to this operator."
                                    });
                                    return;
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            // malformed JSON — log safely
                            if (systemLogger != null)
                            {
                                await systemLogger.LogAsync(
                                    "OperatorValidationMiddleware",
                                    "InvokeAsync",
                                    $"Invalid JSON format in request body: {jsonEx.Message}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (systemLogger != null)
            {
                await systemLogger.LogAsync("OperatorValidationMiddleware", "InvokeAsync", ex.ToString());
            }
        }

        await _next(context);
    }
}
