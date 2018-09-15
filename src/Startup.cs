namespace Sable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using App.Metrics.Health;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup
    {
        public Startup(
            ILifetimeScope webHostScope,
            Serilog.ILogger logger,
            IConfiguration configuration,
            IHealthRoot healthRoot,
            IAccessKeys accessKeys)
        {
            this.webHostScope = webHostScope;
            this.configuration = configuration;
            this.healthRoot = healthRoot;
            this.accessKeys = accessKeys;
            this.logger = logger.ForContext<Startup>();
        }

        private readonly ILifetimeScope webHostScope;
        private readonly IConfiguration configuration;
        private readonly IHealthRoot healthRoot;
        private readonly IAccessKeys accessKeys;
        private ILifetimeScope aspNetScope;
        private readonly Serilog.ILogger logger;

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddControllersAsServices();
            services.AddHealth(healthRoot);
            services.AddHealthEndpoints(configuration.GetSection("health"));

            aspNetScope = webHostScope.BeginLifetimeScope(builder => builder.Populate(services));
            return new AutofacServiceProvider(aspNetScope);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            ConfigureAppLifetime(appLifetime);
            ConfigurePiepine(app, env, accessKeys);
        }

        private void ConfigureAppLifetime(IApplicationLifetime appLifetime)
        {
            appLifetime.ApplicationStarted.Register(() => OnApplicationStarted(appLifetime));
            appLifetime.ApplicationStopping.Register(OnApplicationStopping);
            appLifetime.ApplicationStopped.Register(OnApplicationStopped);
        }

        private static void ConfigurePiepine(IApplicationBuilder app, IHostingEnvironment env, IAccessKeys accessKeys)
        {
            app.UseHostFiltering();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UsePingEndpoint();

            app.UseRequireAccessKey("/health", accessKeys.Create("health"));
            app.UseHealthEndpoint();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        private void OnApplicationStarted(IApplicationLifetime appLifetime)
        {
            try
            {
                var ns = aspNetScope.Resolve<INamingService>();
                ns.RegisterAsync().Wait();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Could not register service within Naming Service, will terminate");
                appLifetime.StopApplication();
            }
        }

        private void OnApplicationStopping()
        {
            var ns = aspNetScope.Resolve<INamingService>();
            ns.DeregisterAsync().Wait();
        }

        private void OnApplicationStopped()
        {
            aspNetScope.Dispose();
        }
    }
}
