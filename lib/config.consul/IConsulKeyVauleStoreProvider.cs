namespace Sable
{
    using System.Threading;
    using System.Threading.Tasks;
    using Consul;
    using Microsoft.Extensions.Primitives;

    public interface IConsulKeyVauleStoreProvider
    {
        Task<(ulong lastIndex, KVPair[] values)> LoadAsync(string prefix, CancellationToken token = default(CancellationToken));

        IChangeToken Watch(string prefix, CancellationToken token = default(CancellationToken));
    }
}
