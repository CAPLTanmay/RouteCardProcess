using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace RouteCardProcess.Middleware
{
    public class SkipJwtForAnonymousMiddleware
    {
        private readonly RequestDelegate _next;

        public SkipJwtForAnonymousMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();

            //  If endpoint is marked [AllowAnonymous], flag to skip JWT validation
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
            {
                context.Items["SkipJwtAuth"] = true;
            }

            await _next(context);
        }
    }
}
