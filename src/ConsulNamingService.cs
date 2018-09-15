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
    using System.Collections.Generic;

    public class ConsulNamingService : INamingService, IDisposable
    {
        public ConsulNamingService(
            IAccessKeys accessKeys,
            Serilog.ILogger logger,
            IOptionsMonitor<NamingServiceOptions> options,
            IOptionsMonitor<RuntimeOptions> runtime)
        {
            this.logger = logger.ForContext<ConsulNamingService>();

            this.accessKeys = accessKeys;
            this.options = (options.CurrentValue, runtime.CurrentValue);
            this.optionsMonitorCallbacks = new List<IDisposable>
            {
                options.OnChange((opt, _) => OnOptionsChange((opt, this.options.runtime))),
                runtime.OnChange((opt, _) => OnOptionsChange((this.options.ns, opt))),
            };

            this.consul = CreateClient(this.options.ns);
        }

        private readonly Serilog.ILogger logger;
        private readonly List<IDisposable> optionsMonitorCallbacks;
        private readonly IAccessKeys accessKeys;
        private (NamingServiceOptions ns, RuntimeOptions runtime) options;
        private ConsulClient consul;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private bool disposed;

        private static string GetInstanceId((NamingServiceOptions ns, RuntimeOptions runtime) opt)
        {
            var (ns, runtime) = opt;
            return $"{ns.Name}-{runtime.Hostname}-{runtime.Pid}-{runtime.Port}";
        }

        private static ConsulClient CreateClient(NamingServiceOptions options)
        {
            var client = new ConsulClient(config =>
            {
                if (options.Address != null) config.Address = options.Address;
            });

            return client;
        }

        private void OnOptionsChange((NamingServiceOptions ns, RuntimeOptions runtime) newOptions)
        {
            bool needToRecreateRegistration = !object.Equals(options.ns.Name, newOptions.ns.Name);
            bool needToReconnect = !object.Equals(options.ns.Address, newOptions.ns.Address);
            bool needToUpdateRegistration =
                  needToRecreateRegistration
                | needToReconnect
                | newOptions != options
                | !Enumerable.SequenceEqual(options.ns.Tags, newOptions.ns.Tags);

            if (!needToReconnect && !needToRecreateRegistration && !needToUpdateRegistration) return;
            this.logger.Warning("Naming Service options changed");

            try
            {
                semaphore.Wait();

                if (needToRecreateRegistration) {
                    DeregisterInternalAsync(consul, options).Wait();
                }

                if (needToReconnect) {
                    this.logger.Information("Connecting to new Naming Service on {Address}", newOptions.ns.Address);
                    consul.Dispose();
                    consul = CreateClient(newOptions.ns);
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

        private async Task RegisterInternalAsync(
            ConsulClient client,
            (NamingServiceOptions ns, RuntimeOptions runtime) options,
            CancellationToken token = default(CancellationToken))
        {
            var (ns, runtime) = options;
            var name = ns.Name;
            var port = runtime.Port;
            var protocol = runtime.Protocol;
            var serviceIp = runtime.ServiceIp;
            var accessKey = accessKeys.Get("health");

            this.logger.Debug("Registering new service {ServiceName} instance on port {ServicePort} in Naming Service",
                name,
                port);

            this.logger.Verbose("Will use {AccessKey} as access key for Health Endpoint", accessKey);

            var serviceId = GetInstanceId(options);
            try
            {
                var baseUrl = $"{protocol}://{serviceIp}:{port}";
                var result = await client.Agent.ServiceRegister(new AgentServiceRegistration
                {
                    ID = serviceId,
                    Name = name,
                    Port = port,
                    Tags = ns.Tags ?? new string[0],
                    Checks = new[] {
                        new NamedAgentServiceCheck
                        {
                            Name = "ping",
                            HTTP = $"{baseUrl}/ping",
                            Interval = ns.CheckInterval,
                            DeregisterCriticalServiceAfter = ns.DeregisterTtl
                        },
                        new NamedAgentServiceCheck
                        {
                            Name = "health",
                            HTTP = $"{baseUrl}/health",
                            Header = {
                                { "X-ACCESS-KEY", new[] { accessKey } }
                            },
                            Interval = ns.CheckInterval,
                            DeregisterCriticalServiceAfter = ns.DeregisterTtl
                        }
                    },
                }, token);

                this.logger.Information("Service instance registered in Naming Service with ID {ServiceId} in {Duration}ms",
                    serviceId,
                    result.RequestTime.TotalMilliseconds);
            }
            catch (System.Exception ex)
            {
                this.logger.Fatal(ex, "Service registration in Naming Service failed, can't continue");
                throw;
            }
        }

        private async Task DeregisterInternalAsync(
            ConsulClient client,
            (NamingServiceOptions ns, RuntimeOptions runtime) options,
            CancellationToken token = default(CancellationToken))
        {
            var (ns, runtime) = options;
            var serviceId = GetInstanceId(options);
            this.logger.Debug("Unregistering service {ServiceName} with ID {ServiceId} instance on port {ServicePort} from Naming Service",
                ns.Name,
                serviceId,
                runtime.Port);

            var result = await client.Agent.ServiceDeregister(serviceId, token);

            this.logger.Information("Service instance with ID {ServiceId} deregistered from Naming Service in {Duration}ms",
                serviceId,
                result.RequestTime.TotalMilliseconds);
        }

        public async Task RegisterAsync(CancellationToken token = default(CancellationToken))
        {
            try
            {
                this.logger.Verbose("Acquiring write lock");
                await semaphore.WaitAsync(token);

                this.logger.Verbose("Lock acquired");
                await RegisterInternalAsync(consul, options, token);
            }
            finally
            {
                semaphore.Release();
                this.logger.Verbose("Lock released");
            }
        }

        public async Task DeregisterAsync(CancellationToken token = default(CancellationToken))
        {
            try
            {
                this.logger.Verbose("Acquiring write lock");
                await semaphore.WaitAsync(token);

                this.logger.Verbose("Lock acquired");
                await DeregisterInternalAsync(consul, options, token);
            }
            finally
            {
                semaphore.Release();
                this.logger.Verbose("Lock released");
            }
        }
         public void Dispose()
        {
            if (disposed) return;

            foreach (var callback in optionsMonitorCallbacks)
                callback.Dispose();

            consul.Dispose();
            semaphore.Dispose();

            disposed = true;
        }
    }
}
