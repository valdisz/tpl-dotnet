namespace Sable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Consul;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    public class PollingConsulKeyVaulePrefixChangeToken : IChangeToken
    {
        public PollingConsulKeyVaulePrefixChangeToken(
            ConsulClient consul,
            string prefix,
            ILogger logger,
            TimeSpan? pollDuration = null,
            CancellationToken token = default(CancellationToken))
        {
            this.consul = consul ?? throw new ArgumentNullException(nameof(consul));
            this.prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            this.pollDuration = pollDuration ?? TimeSpan.FromMinutes(10);

            pollingTask = Task.Factory.StartNew(async () => {
                int failures = 0;
                const int baseFailureDelay = 100;

                while (!token.IsCancellationRequested && !hasChanged)
                {
                    try
                    {
                        (lastUpdateIndex, hasChanged) = await QueryConsulForChangesAsync(consul, prefix, lastUpdateIndex, this.pollDuration);
                        failures = 0;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to load KV store data from Consul");

                        failures++;
                        int delay = baseFailureDelay * (failures + 1);
                        logger.LogDebug(new EventId(1000, "ErrorRecovery"), $"Waiting for {delay}ms before next attempt to connect to Consul KV store");
                        await Task.Delay(delay, token);
                    }
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        private readonly ConsulClient consul;
        private readonly string prefix;
        private readonly TimeSpan pollDuration;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private ulong lastUpdateIndex = ulong.MinValue;
        private bool hasChanged = false;
        private readonly Task pollingTask;

        private static async Task<(ulong lastIndex, bool hasChanges)> QueryConsulForChangesAsync(
            ConsulClient client,
            string prefix,
            ulong updateIndex,
            TimeSpan pollDuration)
        {
            var response = await client.KV.List(prefix, new QueryOptions
            {
                WaitIndex = updateIndex + 1,
                WaitTime = pollDuration
            });

            if (updateIndex == ulong.MinValue)
            {
                // just finished initial request, no changes
                return (response.LastIndex, false);
            }

            return (response.LastIndex, updateIndex != response.LastIndex);
        }

        public bool HasChanged => hasChanged;

        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => EmptyDisposable.Instance;
    }
}
