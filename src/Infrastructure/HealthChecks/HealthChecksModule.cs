namespace Sable
{
    using Autofac;
    using App.Metrics.Health.Builder;
    using App.Metrics.Health;
    using System;

    public interface IHealthEndpoint
    {
        string Name { get; }
        Uri RelativeUri { get; }
        (string name, string[] values)[] Headers { get; }
    }

    public sealed class HealthChecksModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ManualHealthSwitch>()
                .As<IManualHealthSwitch>()
                .SingleInstance();

            builder.Register(ctx => ConfigureHealthChecks(ctx.Resolve<IManualHealthSwitch>()))
                .As<IHealthRoot>()
                .SingleInstance();
        }

        private IHealthRoot ConfigureHealthChecks(IManualHealthSwitch manualHealthSwitch)
        {
            var builder = new HealthBuilder();
            const int threshold = 256 * 1024 * 1024;
            builder
                .HealthChecks.AddProcessPrivateMemorySizeCheck("private-memory", threshold)
                .HealthChecks.AddProcessPhysicalMemoryCheck("working-set", threshold)
                .HealthChecks.AddCheck(manualHealthSwitch as HealthCheck);

            return builder.Build();
        }
    }
}
