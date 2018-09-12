namespace Sable
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
    using Microsoft.AspNetCore.HostFiltering;
    using System.Threading;
    using Autofac.Extensions.DependencyInjection;
    using Serilog.Exceptions;

    public class ServiceHost : IDisposable
    {
        public ServiceHost(ServiceHostOptions options = null)
        {
            var baseLogger = ConfigureLogger(options?.FallbackLogger);
            logger = baseLogger.ForContext<ServiceHost>();

            if (options?.IsDevelopment ?? false)
                logger.Warning("Development mode is enabled");

            logger.Debug("Loading configuration");
            configuration = LoadConfiguration(
                options?.IsDevelopment ?? false,
                options?.Arguments
            );

            logger.Debug("Building DI container");
            container = BuildRootContainer(baseLogger, configuration);
        }

        private readonly Serilog.ILogger logger;
        private readonly IContainer container;
        private readonly IConfiguration configuration;

        private static Serilog.ILogger ConfigureLogger(Serilog.ILogger fallbackLogger)
        {
            return new Serilog.LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithExceptionDetails()
                // .Enrich.WithDemystifiedStackTraces()
                .WriteTo.Logger(fallbackLogger)
                .CreateLogger();
        }

        private static IConfiguration LoadConfiguration(bool isDevelopment, string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            if ((args?.Length ?? 0) > 0) builder.AddCommandLine(args);

            if (isDevelopment) {
                var appAssembly = Assembly.GetExecutingAssembly();
                builder.AddUserSecrets(appAssembly, optional: true);
            }

            return builder.Build();
        }

        private static IContainer BuildRootContainer(Serilog.ILogger logger, IConfiguration configuration)
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(logger).AsImplementedInterfaces();
            builder.RegisterInstance(configuration).AsImplementedInterfaces();
            builder.AddOptions();
            builder.AddConsulNamingService(configuration.GetSection("ns"));

            return builder.Build();
        }

        public async Task StartWebHostAsync(CancellationToken token = default(CancellationToken))
        {
            void WebContainerBuild(ContainerBuilder builder)
            {
                builder.RegisterType<Startup>().AsSelf();
            }

            logger.Information("Starting Web host");
            using (var webHostScope = container.BeginLifetimeScope(WebContainerBuild))
            {
                var webHost = new WebHostBuilder()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseConfiguration(configuration)
                    .UseKestrel((builderContext, options) =>
                    {
                        options.Configure(builderContext.Configuration.GetSection("Kestrel"));
                    })
                    .ConfigureAppConfiguration((builderContext, config) =>
                    {
                        config.AddConfiguration(configuration);
                    })
                    .UseStartup<Startup>()
                    .ConfigureServices((hostingContext, services) => {
                        services.AddTransient(provider => webHostScope.Resolve<Startup>());

                        // Fallback
                        services.PostConfigure<HostFilteringOptions>(options =>
                        {
                            if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
                            {
                                // "AllowedHosts": "localhost;127.0.0.1;[::1]"
                                var hosts = hostingContext.Configuration["AllowedHosts"]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                // Fall back to "*" to disable.
                                options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
                            }
                        });

                        // Change notification
                        services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(
                            new ConfigurationChangeTokenSource<HostFilteringOptions>(hostingContext.Configuration));
                    })
                    .UseDefaultServiceProvider((hostingContext, options) =>
                    {
                        options.ValidateScopes = hostingContext.HostingEnvironment.IsDevelopment();
                    })
                    .UseSerilog(webHostScope.Resolve<Serilog.ILogger>())
                    .Build();

                await webHost.RunAsync(token);
            }
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}
