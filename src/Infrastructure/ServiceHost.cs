namespace Sable
{
    using System;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.AspNetCore.Hosting;
    using System.Threading;
    using Autofac.Extensions.DependencyInjection;
    using Serilog;

    public sealed class ServiceHost : IDisposable
    {
        public ServiceHost(ServiceHostOptions options, Action<ContainerBuilder> configure)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            Logger = options.FallbackLogger.ForContext<ServiceHost>();

            Logger.Debug("Building DI container");
            Container = BuildContainer(options.Arguments, configure);

            Logger = Container
                .Resolve<Serilog.ILogger>()
                .ForContext<ServiceHost>();
        }

        public ILogger Logger { get; }

        public IContainer Container { get; }

        private IContainer BuildContainer(string[] args, Action<ContainerBuilder> callback)
        {
            var builder = new ContainerBuilder();

            builder.AddOptions();
            builder.RegisterModule(new ConfigurationModule(args));
            builder.RegisterModule(new LoggerModule(Logger));

            callback(builder);

            return builder.Build();
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}
