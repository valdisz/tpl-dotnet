namespace Sable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Consul;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    public class ConsulKeyVauleStoreProvider : IConsulKeyVauleStoreProvider
    {
        public ConsulKeyVauleStoreProvider(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        private readonly ConsulClient consul;
        private readonly ILoggerFactory loggerFactory;

        public async Task<(ulong lastIndex, KVPair[] values)> LoadAsync(string prefix, CancellationToken token = default)
        {
            var result = await consul.KV.List(prefix);
            return (result.LastIndex, result.Response);
        }

        public IChangeToken Watch(string prefix, CancellationToken token = default)
            => new PollingConsulKeyVaulePrefixChangeToken(
                consul,
                prefix,
                loggerFactory.CreateLogger<PollingConsulKeyVaulePrefixChangeToken>());
    }
}
