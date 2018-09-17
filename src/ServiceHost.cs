namespace Sable
{
    using System;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.AspNetCore.Hosting;
    using System.Threading;
    using Autofac.Extensions.DependencyInjection;

    public sealed class ServiceHost : IDisposable
    {
        public ServiceHost(ServiceHostOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            logger = options.FallbackLogger.ForContext<ServiceHost>();

            logger.Debug("Building DI container");
            container = BuildContainer(options.Arguments);

            logger = container
                .Resolve<Serilog.ILogger>()
                .ForContext<ServiceHost>();
        }

        private readonly Serilog.ILogger logger;
        private readonly IContainer container;

        private IContainer BuildContainer(string[] args)
        {
            var builder = new ContainerBuilder();

            builder.AddOptions();
            builder.AddConsulNamingService();
            builder.RegisterModule(new LoggerModule(logger));
            builder.RegisterModule(new ConfigurationModule(args));
            builder.RegisterModule(new HealthChecksModule());
            builder.RegisterModule(new WebHostModule());
            builder.RegisterModule(new GraphQLModule<Query, Mutation>());

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
