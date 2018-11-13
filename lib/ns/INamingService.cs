namespace Sable
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface INamingService
    {
        Task RegisterAsync(CancellationToken token = default(CancellationToken));
        Task DeregisterAsync(CancellationToken token = default(CancellationToken));
    }
}
