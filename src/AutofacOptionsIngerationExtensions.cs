namespace Sable
{
    using System;
    using Autofac;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    public static class AutofacOptionsIngerationExtensions
    {
        public static ContainerBuilder AddOptions(this ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(OptionsManager<>))
                .As(typeof(IOptions<>))
                .SingleInstance();

            builder.RegisterGeneric(typeof(OptionsManager<>))
                .As(typeof(IOptionsSnapshot<>))
                .InstancePerLifetimeScope();

            builder.RegisterGeneric(typeof(OptionsMonitor<>))
                .As(typeof(IOptionsMonitor<>))
                .SingleInstance();

            builder.RegisterGeneric(typeof(OptionsFactory<>))
                .As(typeof(IOptionsFactory<>));

            builder.RegisterGeneric(typeof(OptionsCache<>))
                .As(typeof(IOptionsMonitorCache<>))
                .SingleInstance();

            return builder;
        }

        public static ContainerBuilder Configure<TOptions>(this ContainerBuilder builder, string name, IConfiguration config, Action<BinderOptions> configureBinder)
            where TOptions : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            builder.Register(ctx => new ConfigurationChangeTokenSource<TOptions>(name, config))
                .As<IOptionsChangeTokenSource<TOptions>>()
                .SingleInstance();

            builder.Register(ctx => new NamedConfigureFromConfigurationOptions<TOptions>(name, config, configureBinder))
                .As<IConfigureOptions<TOptions>>()
                .SingleInstance();

            return builder;
        }

        public static ContainerBuilder Configure<TOptions>(this ContainerBuilder builder, IConfiguration config) where TOptions : class
            => builder.Configure<TOptions>(Options.DefaultName, config);

        public static ContainerBuilder Configure<TOptions>(this ContainerBuilder builder, string name, IConfiguration config) where TOptions : class
            => builder.Configure<TOptions>(name, config, _ => { });

        public static ContainerBuilder Configure<TOptions>(this ContainerBuilder builder, IConfiguration config, Action<BinderOptions> configureBinder)
            where TOptions : class
            => builder.Configure<TOptions>(Options.DefaultName, config, configureBinder);
    }
}
