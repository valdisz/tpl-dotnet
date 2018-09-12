namespace Sable
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;

    public class ConsulNamingService : INamingService
    {
        public ConsulNamingService(Serilog.ILogger logger, IOptions<NamingServiceOptions> options)
        {
            this.logger = logger.ForContext<ConsulNamingService>();
            this.options = options.Value;
        }

        private readonly Serilog.ILogger logger;
        private NamingServiceOptions options;

        public Task RegisterAsync()
        {
            this.logger.Information("Registering new service {ServiceName} instance", options.Name);
            return Task.CompletedTask;
        }

        public Task UnregisterAsync()
        {
            this.logger.Information($"Unregistering service {options.Name} instance");
            return Task.CompletedTask;
        }
    }
}
