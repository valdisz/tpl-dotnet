namespace Sable
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public sealed class RequireAccessKeyMiddleware
    {
        public RequireAccessKeyMiddleware(PathString path, string accessKey, Serilog.ILogger logger, RequestDelegate next)
        {
            this.accessKey = accessKey;
            this.path = path;
            this.logger = logger.ForContext<RequireAccessKeyMiddleware>();
            this.next = next;
        }

        private readonly string accessKey;
        private readonly PathString path;
        private readonly Serilog.ILogger logger;
        private readonly RequestDelegate next;

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments(path))
            {
                await next(context);
                return;
            }

            if (context.User.Identity.IsAuthenticated)
            {
                await next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-ACCESS-KEY", out var providedKey))
            {
                logger.Warning("Access key was not provided");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (!accessKey.Equals(providedKey))
            {
                logger.Warning("Access key does not match");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next(context);
        }
    }
}
