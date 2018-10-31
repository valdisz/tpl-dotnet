namespace Sable
{
    using System;
    using System.Threading.Tasks;
    using Autofac;
    using Serilog;
    using Serilog.Sinks.SystemConsole.Themes;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var consoleLogger = new Serilog.LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(
                    theme: AnsiConsoleTheme.Literate,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            var logger = consoleLogger.ForContext<Program>();

            try
            {
                logger.Information("Starting up");

                var options = new ServiceHostOptions
                {
                    Arguments = args,
                    FallbackLogger = consoleLogger
                };

                using (var hostInstance = new ServiceHost(options, builder =>
                {
                    builder.AddConsulNamingService();
                    builder.AddHealthChecks();
                    builder.AddWebHost<Startup>();
                    builder.AddGrapqhQL<Query, Mutation>();
                }))
                {
                    await hostInstance.StartWebHostAsync();
                    return 0;
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Web host terminated unexpectedly");
                return 1;
            }
        }
    }
}
