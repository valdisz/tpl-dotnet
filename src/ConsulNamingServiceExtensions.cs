namespace Sable
{
    using Autofac;
    using Microsoft.Extensions.Configuration;

    public static class ConsulNamingServiceExtensions
    {
        public static ContainerBuilder AddConsulNamingService(this ContainerBuilder builder, IConfiguration config)
        {
            builder.Configure<NamingServiceOptions>(config.GetSection("ns"));
            builder.PostConfigure<NamingServiceOptions>(options =>
            {
                options.Protocol = config.GetValue<string>(ServiceHost.PROP_PROTOCOL);
                options.Port = config.GetValue<int>(ServiceHost.PROP_PORT);
                options.Pid = config.GetValue<int>(ServiceHost.PROP_PID);
                options.Hostname = config.GetValue<string>(ServiceHost.PROP_HOSTNAME);
                options.ServiceIp = config.GetValue<string>(ServiceHost.PROP_SERVICE_IP);
            });

            builder.RegisterType<ConsulNamingService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            return builder;
        }
    }
}
