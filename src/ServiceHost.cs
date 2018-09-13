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
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Diagnostics;

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

        public const string PROP_PROTOCOL = "protocol";
        public const string PROP_PORT = "port";
        public const string PROP_INTERFACE = "interface";
        public const string PROP_HOSTNAME = "hostname";
        public const string PROP_SERVICE_IP = "serviceIp";
        public const string PROP_PID = "pid";

        private readonly Serilog.ILogger logger;
        private readonly IContainer container;
        private readonly IConfiguration configuration;

        private static Serilog.ILogger ConfigureLogger(Serilog.ILogger fallbackLogger)
        {
            return new Serilog.LoggerConfiguration()
                .MinimumLevel.Verbose()
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
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { PROP_PROTOCOL, "http" },
                    { PROP_PORT, "5000" },
                    { PROP_INTERFACE, "0.0.0.0" },
                    { PROP_HOSTNAME, System.Environment.MachineName },
                    { PROP_SERVICE_IP, GetLocalIPAddress() }
                })
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            if ((args?.Length ?? 0) > 0) builder.AddCommandLine(args);

            if (isDevelopment) {
                var appAssembly = Assembly.GetExecutingAssembly();
                builder.AddUserSecrets(appAssembly, optional: true);
            }

            builder.AddInMemoryCollection(new Dictionary<string, string> {
                { PROP_PID, Process.GetCurrentProcess().Id.ToString() }
            });

            var config = builder.Build();

            var protocol = config.GetValue<string>("protocol");
            var port = config.GetValue<int>("port");
            var @interface = config.GetValue<string>("interface");
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "urls", $"{protocol}://{@interface}:{port}" }
                })
                .AddConfiguration(config)
                .Build();
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static IContainer BuildRootContainer(Serilog.ILogger logger, IConfiguration configuration)
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(logger).AsImplementedInterfaces();
            builder.RegisterInstance(configuration).AsImplementedInterfaces();
            builder.AddOptions();
            builder.AddConsulNamingService(configuration);

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
