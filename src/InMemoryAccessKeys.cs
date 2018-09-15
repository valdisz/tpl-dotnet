namespace Sable
{
    using System;
    using System.Collections.Generic;

    public class InMemoryAccessKeys : IAccessKeys
    {
        private Dictionary<string, string> keys = new Dictionary<string, string>();

        public string Create(string name)
        {
            var key = Guid.NewGuid().ToString("N");
            keys[name] = key;

            return key;
        }

        public string Get(string name) =>
            keys.TryGetValue(name, out var key)
                ? key
                : null;
    }
}
