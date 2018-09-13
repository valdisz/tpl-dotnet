namespace Sable
{
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;

    public class WebContainerModule : Autofac.Module
    {
        public void ConfigureServices(IServiceCollection services)
        {

        }

        protected override void Load(ContainerBuilder builder)
        {


            var services = new ServiceCollection();
            ConfigureServices(services);
            builder.Populate(services);
        }
    }
}
