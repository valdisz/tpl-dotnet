namespace Sable
{
    using System.Threading.Tasks;

    public interface INamingService
    {
        Task RegisterAsync();
        Task UnregisterAsync();
    }
}
