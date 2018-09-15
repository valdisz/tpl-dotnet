namespace Sable
{
    using System;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.AspNetCore.Hosting;
    using System.Threading;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Builder;

    public interface IHealthEndpointAccessKey
    {
        string Key { get; }
    }

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

    public static class RequireAccessKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequireAccessKey(this IApplicationBuilder builder, PathString path, string accessKey)
        {
            return builder.UseMiddleware<RequireAccessKeyMiddleware>(path, accessKey);
        }
    }

    public sealed class ServiceHost : IDisposable
    {
        public ServiceHost(ServiceHostOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            logger = options.FallbackLogger.ForContext<ServiceHost>();
            logger.Debug("Building DI container");

            var builder = new ContainerBuilder();

            builder.AddOptions();
            builder.AddConsulNamingService();
            builder.RegisterModule(new LoggerModule(logger));
            builder.RegisterModule(new ConfigurationModule(options.Arguments));
            builder.RegisterModule(new HealthChecksModule());
            builder.RegisterModule(new WebHostModule());

            container = builder.Build();

            logger = container
                .Resolve<Serilog.ILogger>()
                .ForContext<ServiceHost>();
        }

        private readonly Serilog.ILogger logger;
        private readonly IContainer container;

        public async Task StartWebHostAsync(CancellationToken token = default(CancellationToken))
        {
            void WebContainerBuild(ContainerBuilder builder)
            {
                builder.RegisterType<Startup>().AsSelf();
            }

            logger.Information("Starting Web host");
            using (var webHostScope = container.BeginLifetimeScope(WebContainerBuild))
            {
                var webHost = webHostScope.Resolve<IWebHost>();
                await webHost.RunAsync(token);
            }
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}
