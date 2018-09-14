namespace Sable
{
    using Autofac;
    using Serilog;
    using Serilog.Exceptions;

    public sealed class LoggerModule : Autofac.Module
    {
        public LoggerModule(Serilog.ILogger fallbackLogger)
        {
            this.fallbackLogger = fallbackLogger;
        }

        private readonly Serilog.ILogger fallbackLogger;

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(ctx =>
                new Serilog.LoggerConfiguration()
                    // .MinimumLevel.Verbose()
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithProcessId()
                    .Enrich.WithThreadId()
                    .Enrich.WithExceptionDetails()
                    // .Enrich.WithDemystifiedStackTraces()
                    .WriteTo.Logger(fallbackLogger)
                    .CreateLogger()
                )
                .As<Serilog.ILogger>()
                .SingleInstance();
        }
    }
}
