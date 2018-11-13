namespace Sable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Consul;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;

    public sealed class ConsulConfigurationProvider : ConfigurationProvider
    {
        public ConsulConfigurationProvider(
            IConsulKeyVauleManager kvManager,
            IConsulKeyVauleStoreProvider kvProvider)
        {
            ChangeToken.OnChange(
                () => kvProvider.Watch(kvManager.Prefix),
                () => {
                    Load();
                    OnReload();
                }
            );
        }

        private readonly ConsulClient consul;
        private readonly IConsulKeyVauleManager kvManager;

        public override void Load()
        {
            Data = LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task<Dictionary<string, string>> LoadAsync()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var list = await consul.KV.List(kvManager.Prefix);
            foreach (var kv in list.Response)
            {
                if (!kvManager.ShouldLoad(kv.Key)) continue;

                var key = kvManager.MapKey(kv.Key);
                var value = kvManager.DecodeValue(kv.Value);
                data.Add(key, value);
            }

            return data;
        }
    }
}
