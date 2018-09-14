using System;

namespace Sable
{
    public class NamingServiceOptions
    {
        public string Name { get; set; }
        public string[] Tags { get; set; }
        public TimeSpan? CheckInterval { get; set; }
        public TimeSpan? DeregisterTtl { get; set; }
        public int Port { get; set; }
        public int Pid { get; set; }
        public string Hostname { get; set; }
        public string ServiceIp { get; set; }
        public Uri Address { get; set; }
        public string Protocol { get;  set; }
        public string AccessKey { get; set; }
    }
}
