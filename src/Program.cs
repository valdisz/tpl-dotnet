namespace Sable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.HostFiltering;
    using Microsoft.Extensions.Options;
    using Serilog;
    using Microsoft.AspNetCore.Hosting.Internal;
    using clipr;
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

                using (var instance = new ServiceHost(new ServiceHostOptions
                {
                    Arguments = args,
                    FallbackLogger = consoleLogger
                }))
                {
                    await instance.StartWebHostAsync();
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
