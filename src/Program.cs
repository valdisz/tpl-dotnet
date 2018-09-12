namespace tpl_dotnet
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

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var consoleLogger = new Serilog.LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                consoleLogger.Information("Starting up");
                var options = ParseArguments(args);

                using (var instance = new Service(new ServiceOptions
                {
                    IsDevelopment = options.IsDevelopment,
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
                consoleLogger.Fatal(ex, "Web host terminated unexpectedly");
                return 1;
            }
        }

        public static StartOptions ParseArguments(string[] args)
        {
            return CliParser.TryParse<StartOptions>(args, out var options)
                ? options
                : new StartOptions();
        }
    }
}
