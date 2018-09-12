namespace tpl_dotnet
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
    using Microsoft.AspNetCore.HostFiltering;
    using Microsoft.Extensions.Options;
    using System.Threading;

    public class Service : IDisposable
    {
        public Service(ServiceOptions options = null)
        {
            Logger = ConfigureLogger(options?.FallbackLogger);

            if (options?.IsDevelopment ?? false) Logger.Warning("Development mode is enabled");

            Logger.Debug("Loading configuration");
            configuration = LoadConfiguration(
                options?.IsDevelopment ?? false,
                options?.Arguments
            );

            Logger.Debug("Building DI container");
            container = BuildRootContainer(Logger, configuration);
        }

        public Serilog.ILogger Logger { get; }
        private readonly IContainer container;
        private readonly IConfiguration configuration;

        private static Serilog.ILogger ConfigureLogger(Serilog.ILogger fallbackLogger)
        {
            return new Serilog.LoggerConfiguration()
                .WriteTo.Logger(fallbackLogger)
                .CreateLogger();
        }

        private static IConfiguration LoadConfiguration(bool isDevelopment, string[] args)
        {
            var builder = new ConfigurationBuilder()
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
            var di = new ContainerBuilder();
            di.RegisterInstance(logger).AsImplementedInterfaces();
            di.RegisterInstance(configuration).AsImplementedInterfaces();

            return di.Build();
        }

        public async Task StartWebHostAsync(CancellationToken token = default(CancellationToken))
        {
            void WebContainerBuild(ContainerBuilder builder)
            {
                builder.RegisterType<Startup>().AsSelf();
            }

            Logger.Information("Starting Web host");
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
                    .UseSerilog(Logger)
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
