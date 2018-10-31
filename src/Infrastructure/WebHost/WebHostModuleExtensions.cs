namespace Sable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.AspNetCore.Hosting;

    public static class WebHostModuleExtensions
    {
        public static ContainerBuilder AddWebHost<TStartup>(this ContainerBuilder builder)
            where TStartup: class
        {
            builder.RegisterModule(new WebHostModule<TStartup>());
            return builder;
        }

        public static async Task StartWebHostAsync(this ServiceHost host, CancellationToken token = default(CancellationToken))
        {
            host.Logger.Information("Starting Web host");
            using (var webHostScope = host.Container.BeginLifetimeScope())
            {
                var webHost = webHostScope.Resolve<IWebHost>();
                await webHost.RunAsync(token);
            }
        }
    }
}
