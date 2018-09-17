namespace Sable
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Builder;

    public static class RequireAccessKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequireAccessKey(this IApplicationBuilder builder, PathString path, string accessKey)
        {
            return builder.UseMiddleware<RequireAccessKeyMiddleware>(path, accessKey);
        }
    }
}
