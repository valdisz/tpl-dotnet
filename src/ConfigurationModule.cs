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
    using Microsoft.Extensions.Configuration.Memory;

    public interface IRuntimeConfiguration
    {
        void Set(string key, string value);
        bool TryGet(string key, out string value);
    }

    public sealed class RuntimeConfiguration : IRuntimeConfiguration, IConfigurationSource
    {
        public RuntimeConfiguration()
        {
            source = new MemoryConfigurationSource();
            provider = new MemoryConfigurationProvider(source);
        }

        private readonly MemoryConfigurationSource source;
        private readonly MemoryConfigurationProvider provider;

        public void Set(string key, string value)
            => provider.Set(key, value);

        public bool TryGet(string key, out string value)
            => provider.TryGet(key, out value);

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return provider;
        }
    }

    public sealed class ConfigurationModule : Autofac.Module
    {
        public ConfigurationModule(string[] args)
        {
            this.args = args;
        }

        public const string PROP_PROTOCOL = "protocol";
        public const string PROP_PORT = "port";
        public const string PROP_INTERFACE = "interface";
        public const string PROP_HOSTNAME = "hostname";
        public const string PROP_SERVICE_IP = "serviceIp";
        public const string PROP_PID = "pid";
        public const string PROP_DEVELOPMENT = "dev";

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
        }

        private static IConfiguration LoadConfiguration(string[] args, IConfigurationSource runtimeConfiguration)
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

            IConfiguration cliConfig = null;
            if ((args?.Length ?? 0) > 0)
            {
                cliConfig = new ConfigurationBuilder()
                    .AddCommandLine(
                        args,
                        new Dictionary<string, string>
                        {
                            { "d", PROP_DEVELOPMENT }
                        })
                    .Build();
                builder.AddConfiguration(cliConfig);
            }

            if (cliConfig?.GetValue<bool>(PROP_DEVELOPMENT, false) ?? false) {
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
