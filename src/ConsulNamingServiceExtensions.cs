namespace Sable
{
    using Autofac;
    using Microsoft.Extensions.Configuration;

    public static class ConsulNamingServiceExtensions
    {
        public static ContainerBuilder AddConsulNamingService(this ContainerBuilder builder)
        {
            builder.Configure<NamingServiceOptions>(config => config.GetSection("ns"));
            builder.PostConfigure<NamingServiceOptions>((config, options) =>
            {
                options.Protocol = config.GetValue<string>(ConfigurationModule.PROP_PROTOCOL);
                options.Port = config.GetValue<int>(ConfigurationModule.PROP_PORT);
                options.Pid = config.GetValue<int>(ConfigurationModule.PROP_PID);
                options.Hostname = config.GetValue<string>(ConfigurationModule.PROP_HOSTNAME);
                options.ServiceIp = config.GetValue<string>(ConfigurationModule.PROP_SERVICE_IP);
                options.AccessKey = config.GetValue<string>("access-key");
            });

            builder.RegisterType<ConsulNamingService>()
                .AsImplementedInterfaces()
                .SingleInstance();

            return builder;
        }
    }
}
