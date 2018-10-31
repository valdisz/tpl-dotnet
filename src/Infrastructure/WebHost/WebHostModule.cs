namespace Sable
{
    using System;
    using System.IO;
    using Autofac;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
    using Microsoft.AspNetCore.HostFiltering;

    public sealed class WebHostModule<TStartup> : Autofac.Module
        where TStartup: class
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TStartup>().AsSelf();

            builder.Register(ctx =>
            {
                var configuration = ctx.Resolve<IConfiguration>();
                var defaultLogger = ctx.Resolve<Serilog.ILogger>();

                return new WebHostBuilder()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseConfiguration(configuration)
                    .UseKestrel((builderContext, options) =>
                        options.Configure(builderContext.Configuration.GetSection("Kestrel")))
                    .ConfigureAppConfiguration((builderContext, config) =>
                        config.AddConfiguration(configuration))
                    .UseStartup<TStartup>()
                    .ConfigureServices((hostingContext, services) => {
                        services.AddTransient(provider => ctx.Resolve<TStartup>());

                        // Fallback
                        services.PostConfigure<HostFilteringOptions>(options =>
                        {
                            if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
                            {
                                // "AllowedHosts": "localhost;127.0.0.1;[::1]"
                                var hosts = hostingContext.Configuration["AllowedHosts"]
                                    ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                // Fall back to "*" to disable.
                                options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
                            }
                        });

                        // Change notification
                        services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(
                            new ConfigurationChangeTokenSource<HostFilteringOptions>(hostingContext.Configuration));
                    })
                    .UseDefaultServiceProvider((hostingContext, options) =>
                    {
                        options.ValidateScopes = hostingContext.HostingEnvironment.IsDevelopment();
                    })
                    .UseSerilog(defaultLogger)
                    .Build();
            })
            .As<IWebHost>()
            .InstancePerLifetimeScope();
        }
    }
}
