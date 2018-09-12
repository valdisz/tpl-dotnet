namespace Sable
{
    using Autofac;
    using Microsoft.Extensions.Configuration;

    public static class ConsulNamingServiceExtensions
    {
        public static ContainerBuilder AddConsulNamingService(this ContainerBuilder builder, IConfiguration config)
        {
            builder.Configure<NamingServiceOptions>(config);
            builder.RegisterType<ConsulNamingService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            return builder;
        }
    }
}
