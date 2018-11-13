namespace Sable
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DefaultConsulKeyVauleManager : IConsulKeyVauleManager
    {
        public DefaultConsulKeyVauleManager(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; }

        public string DecodeValue(byte[] value) => Encoding.UTF8.GetString(value);

        public string MapKey(string key) => key.Remove(0, Prefix.Length + 1).Replace('/', ':');

        public bool ShouldLoad(string key) => key.StartsWith(Prefix);
    }
}
