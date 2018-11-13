namespace Sable
{
    using Consul;
    using System.Collections.Generic;

    public class NamedAgentServiceCheck : AgentServiceCheck
    {
        public string Name { get; set; }

        public Dictionary<string, string[]> Header { get; set; }
            = new Dictionary<string, string[]>();
    }
}
