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

        public static ContainerBuilder Configure<TOptions>(this ContainerBuilder builder, string name, Func<IConfiguration, IConfiguration> config, Action<BinderOptions> configureBinder)
            where TOptions : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Register(ctx =>
                {
                    var configuration = config.Invoke(ctx.Resolve<IConfiguration>());
                    return new ConfigurationChangeTokenSource<TOptions>(name, configuration);
                })
                .As<IOptionsChangeTokenSource<TOptions>>()
                .SingleInstance();

            builder.Register(ctx =>
                {
                    var configuration = config.Invoke(ctx.Resolve<IConfiguration>());
                    return new NamedConfigureFromConfigurationOptions<TOptions>(name, configuration, configureBinder);
                })
                .As<IConfigureOptions<TOptions>>()
                .SingleInstance();

            return builder;
        }

        public static ContainerBuilder Configure<TOptions>(this ContainerBuilder builder, Func<IConfiguration, IConfiguration> config) where TOptions : class
            => builder.Configure<TOptions>(Options.DefaultName, config);

        public static ContainerBuilder Configure<TOptions>(this ContainerBuilder builder, string name, Func<IConfiguration, IConfiguration> config) where TOptions : class
            => builder.Configure<TOptions>(name, config, _ => { });

        public static ContainerBuilder Configure<TOptions>(this ContainerBuilder builder, Func<IConfiguration, IConfiguration> config, Action<BinderOptions> configureBinder)
            where TOptions : class
            => builder.Configure<TOptions>(Options.DefaultName, config, configureBinder);

        public static ContainerBuilder PostConfigure<TOptions>(this ContainerBuilder builder, string name, Action<TOptions> configureOptions)
            where TOptions : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            builder.Register(ctx => new PostConfigureOptions<TOptions>(name, configureOptions))
                .As<IPostConfigureOptions<TOptions>>()
                .SingleInstance();

            return builder;
        }

        public static ContainerBuilder PostConfigure<TOptions>(this ContainerBuilder builder, Action<TOptions> configureOptions) where TOptions : class
            => builder.PostConfigure(Options.DefaultName, configureOptions);

        public static ContainerBuilder PostConfigure<TOptions>(this ContainerBuilder builder, string name, Action<IConfiguration, TOptions> configureOptions)
            where TOptions : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            builder.Register(ctx => {
                var config = ctx.Resolve<IConfiguration>();
                return new PostConfigureOptions<TOptions>(name, options => configureOptions(config, options));
            })
                .As<IPostConfigureOptions<TOptions>>()
                .SingleInstance();

            return builder;
        }

        public static ContainerBuilder PostConfigure<TOptions>(this ContainerBuilder builder, Action<IConfiguration, TOptions> configureOptions) where TOptions : class
            => builder.PostConfigure(Options.DefaultName, configureOptions);
    }
}
