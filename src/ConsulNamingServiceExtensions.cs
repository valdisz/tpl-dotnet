namespace Sable
{
    using Autofac;
    using Microsoft.Extensions.Configuration;

    public static class ConsulNamingServiceExtensions
    {
        public static ContainerBuilder AddConsulNamingService(this ContainerBuilder builder)
        {
            builder.Configure<NamingServiceOptions>(config => config.GetSection("ns"));

            builder.RegisterType<ConsulNamingService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            return builder;
        }
    }
}
