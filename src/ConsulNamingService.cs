namespace Sable
{
    using Consul;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using System.Threading;
    using System.Net;
    using System.Net.Sockets;
    using System.Linq;

    public class ConsulNamingService : INamingService, IDisposable
    {
        public ConsulNamingService(Serilog.ILogger logger,
            IOptions<NamingServiceOptions> options,
            IOptionsMonitor<NamingServiceOptions> optionsMonitor)
        {
            this.logger = logger.ForContext<ConsulNamingService>();
            this.options = options.Value;
            this.optionsMonitorCallback = optionsMonitor.OnChange(OnOptionsChange);

            this.consul = CreateClient(this.options);
        }

        static readonly TimeSpan DEFAULT_CHECK_INTERVAL = TimeSpan.FromSeconds(30);
        static readonly TimeSpan DEFAULT_DEREGISTER_TTL = TimeSpan.FromSeconds(600);

        private readonly Serilog.ILogger logger;
        private readonly IDisposable optionsMonitorCallback;
        private NamingServiceOptions options;
        private ConsulClient consul;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private bool disposed;

        private static string GetInstanceId(NamingServiceOptions options)
            => $"{options.Name}-{options.Hostname}-{options.Pid}-{options.Port}";

        private static ConsulClient CreateClient(NamingServiceOptions options)
        {
            var client = new ConsulClient(config =>
            {
                if (options.Address != null) config.Address = options.Address;
            });

            return client;
        }

        private void OnOptionsChange(NamingServiceOptions newOptions, string name)
        {
            bool needToRecreateRegistration = !object.Equals(options.Name, newOptions.Name);
            bool needToUpdateRegistration =
                  needToRecreateRegistration
                | !object.Equals(options.CheckInterval, newOptions.CheckInterval)
                | !object.Equals(options.DeregisterTtl, newOptions.DeregisterTtl)
                | !Enumerable.SequenceEqual(options.Tags ?? Enumerable.Empty<string>(), newOptions.Tags ?? Enumerable.Empty<string>());
            bool needToReconnect = !object.Equals(options.Address, newOptions.Address);

            if (!needToReconnect && !needToRecreateRegistration && !needToUpdateRegistration) return;
            this.logger.Warning("Naming Service options changed");

            try
            {
                semaphore.Wait();

                if (needToRecreateRegistration) {
                    DeregisterInternalAsync(consul, options).Wait();
                }

                if (needToReconnect) {
                    this.logger.Information("Connecting to new Naming Service on {Address}", newOptions.Address);
                    consul.Dispose();
                    consul = CreateClient(newOptions);
                }

                if (needToUpdateRegistration) {
                    RegisterInternalAsync(consul, newOptions).Wait();
                };

                options = newOptions;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task RegisterInternalAsync(ConsulClient client, NamingServiceOptions options, CancellationToken token = default(CancellationToken))
        {
            var name = options.Name;
            var port = options.Port;
            var protocol = options.Protocol;
            var serviceIp = options.ServiceIp;

            this.logger.Information("Registering new service {ServiceName} instance on port {ServicePort} in Naming Service",
                name,
                port);

            var serviceId = GetInstanceId(options);
            try
            {
                var result = await client.Agent.ServiceRegister(new AgentServiceRegistration
                {
                    ID = serviceId,
                    Name = name,
                    Port = port,
                    Tags = options.Tags ?? new string[0],
                    Check = new AgentServiceCheck
                    {
                        HTTP = $"{protocol}://{serviceIp}:{port}/ping",
                        Interval = options.CheckInterval ?? DEFAULT_CHECK_INTERVAL,
                        DeregisterCriticalServiceAfter = options.DeregisterTtl ?? DEFAULT_DEREGISTER_TTL
                    }
                }, token);

                this.logger.Debug("Service instance registered in Naming Service with ID {ServiceId} in {Duration}ms",
                    serviceId,
                    result.RequestTime.TotalMilliseconds);
            }
            catch (System.Exception ex)
            {
                this.logger.Fatal(ex, "Service registration in Naming Service failed, can't continue");
                throw;
            }
        }

        private async Task DeregisterInternalAsync(ConsulClient client, NamingServiceOptions options, CancellationToken token = default(CancellationToken))
        {
            var serviceId = GetInstanceId(options);
            this.logger.Information("Unregistering service {ServiceName} with ID {ServiceId} instance on port {ServicePort} from Naming Service",
                options.Name,
                serviceId,
                options.Port);

            var result = await client.Agent.ServiceDeregister(serviceId, token);

            this.logger.Debug("Service instance with ID {ServiceId} deregistered from Naming Service in {Duration}ms",
                serviceId,
                result.RequestTime.TotalMilliseconds);
        }

        public async Task RegisterAsync(CancellationToken token = default(CancellationToken))
        {
            try
            {
                this.logger.Debug("Acquiring write lock");
                await semaphore.WaitAsync(token);

                this.logger.Debug("Lock acquired");
                await RegisterInternalAsync(consul, options, token);
            }
            finally
            {
                semaphore.Release();
                this.logger.Debug("Lock released");
            }
        }

        public async Task DeregisterAsync(CancellationToken token = default(CancellationToken))
        {
            try
            {
                this.logger.Debug("Acquiring write lock");
                await semaphore.WaitAsync(token);

                this.logger.Debug("Lock acquired");
                await DeregisterInternalAsync(consul, options, token);
            }
            finally
            {
                semaphore.Release();
                this.logger.Debug("Lock released");
            }
        }

        public void Dispose()
        {
            if (disposed) return;

            optionsMonitorCallback.Dispose();
            consul.Dispose();
            semaphore.Dispose();

            disposed = true;
        }
    }
}
