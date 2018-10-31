namespace Sable
{
    using Autofac;

    public static class HealthChecksModuleExtensions
    {
        public static ContainerBuilder AddHealthChecks(this ContainerBuilder builder)
        {
            builder.RegisterModule(new HealthChecksModule());
            return builder;
        }
    }
}
