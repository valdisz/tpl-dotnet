namespace Sable
{
    using System;
    using System.Threading.Tasks;
    using Consul;
    using Microsoft.Extensions.Logging;
    using Xunit;

    public class PollingConsulKeyVaulePrefixChangeTokenFacts
    {
        [Fact]
        public async void Foo()
        {
            var consul = new ConsulClient();
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<PollingConsulKeyVaulePrefixChangeToken>();
            var token = new PollingConsulKeyVaulePrefixChangeToken(consul, "test", logger);

            await Task.Delay(10000);

            Assert.True(token.HasChanged);
        }
    }
}
