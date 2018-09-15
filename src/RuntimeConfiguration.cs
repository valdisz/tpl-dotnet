namespace Sable
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;

    public sealed class RuntimeConfiguration : IRuntimeConfiguration, IConfigurationSource
    {
        public RuntimeConfiguration()
        {
            source = new MemoryConfigurationSource();
            provider = new MemoryConfigurationProvider(source);
        }

        private readonly MemoryConfigurationSource source;
        private readonly MemoryConfigurationProvider provider;

        public void Set(string key, string value)
            => provider.Set(key, value);

        public bool TryGet(string key, out string value)
            => provider.TryGet(key, out value);

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return provider;
        }
    }
}
