namespace Sable
{
    using System;
    using System.IO;
    using System.Reflection;
    using Autofac;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Diagnostics;
    using Microsoft.Extensions.Primitives;

    public sealed class ConfigurationModule : Autofac.Module
    {
        public ConfigurationModule(string[] args)
        {
            this.args = args;
        }

        public const string DEFAULT_PROTOCOL = "http";
        public const string DEFAULT_INTERFACE = "0.0.0.0";
        public const int DEFAULT_PORT = 5000;

        private readonly string[] args;

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RuntimeConfiguration>()
                .As<IRuntimeConfiguration>()
                .SingleInstance();

            builder.Register(ctx =>
                {
                    ctx.Resolve<Serilog.ILogger>()
                        .ForContext<ConfigurationModule>()
                        .Debug("Loading configuration");

                    var runtimeConfiguration = ctx.Resolve<IRuntimeConfiguration>() as IConfigurationSource;

                    return LoadConfiguration(args, runtimeConfiguration);
                })
                .As<IConfiguration>()
                .SingleInstance();

            builder.RegisterType<InMemoryAccessKeys>()
                .As<IAccessKeys>()
                .SingleInstance();

            builder.Configure<RuntimeOptions>(config => config.GetSection(RuntimeOptions.SECTION));
        }

        private static IConfiguration LoadConfiguration(string[] args, IConfigurationSource runtimeConfiguration)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Protocol)}", DEFAULT_PROTOCOL },
                    { $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Port)}", DEFAULT_PORT.ToString() },
                    { $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Interface)}", DEFAULT_INTERFACE },
                    { $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Hostname)}", System.Environment.MachineName },
                    { $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.ServiceIp)}", GetLocalIPAddress() }
                })
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfiguration cliConfig = null;
            if ((args?.Length ?? 0) > 0)
            {
                cliConfig = new ConfigurationBuilder()
                    .AddCommandLine(
                        args,
                        new Dictionary<string, string>
                        {
                            { "-d", $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Development)}" },
                            { "--dev", $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Development)}" },
                            { "-p", $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Port)}" },
                            { "--port", $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Port)}" }
                        })
                    .Build();
                builder.AddConfiguration(cliConfig);
            }

            if (cliConfig?.GetSection(RuntimeOptions.SECTION)?.GetValue<bool>(nameof(RuntimeOptions.Development), false) ?? false) {
                var appAssembly = Assembly.GetExecutingAssembly();
                builder.AddUserSecrets(appAssembly, optional: true);
            }

            builder.AddInMemoryCollection(new Dictionary<string, string> {
                { $"{RuntimeOptions.SECTION}:{nameof(RuntimeOptions.Pid)}", Process.GetCurrentProcess().Id.ToString() }
            });

            var config = builder.Build();

            var runtime = config.GetSection(RuntimeOptions.SECTION).Get<RuntimeOptions>();
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "urls", $"{runtime.Protocol}://{runtime.Interface}:{runtime.Port}" }
                })
                .AddConfiguration(config)
                .Add(runtimeConfiguration)
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
    }
}
