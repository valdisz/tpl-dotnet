namespace tpl_dotnet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup
    {
        public Startup(ILifetimeScope webHostScope)
        {
            this.webHostScope = webHostScope;
        }

        private readonly ILifetimeScope webHostScope;
        private ILifetimeScope aspNetScope;

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddControllersAsServices();

            aspNetScope = webHostScope.BeginLifetimeScope(builder => builder.Populate(services));
            return new AutofacServiceProvider(aspNetScope);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            ConfigureAppLifetime(appLifetime);
            ConfigurePiepine(app, env);
        }

        private void ConfigureAppLifetime(IApplicationLifetime appLifetime)
        {
            appLifetime.ApplicationStopped.Register(() => aspNetScope.Dispose());
        }

        private static void ConfigurePiepine(IApplicationBuilder app, IHostingEnvironment env)
        {
            // app.UseHostFiltering();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
