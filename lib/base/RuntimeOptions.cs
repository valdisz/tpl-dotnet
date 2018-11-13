namespace Sable
{
    using System;

    public sealed class RuntimeOptions : IEquatable<RuntimeOptions>
    {
        public const string SECTION = "runtime";

        public string Protocol { get; set; }
        public int Port { get; set; }
        public string Interface { get; set; }
        public string Hostname { get; set; }
        public string ServiceIp { get; set; }
        public string Pid { get; set; }
        public bool Development { get; set; }

        public static bool operator ==(RuntimeOptions left, RuntimeOptions right) =>
            Equals(left, right);

        public static bool operator !=(RuntimeOptions left, RuntimeOptions right) =>
            !Equals(left, right);

        public override bool Equals(object obj) =>
            (obj is RuntimeOptions metrics) && Equals(metrics);

        public bool Equals(RuntimeOptions other) =>
            (Protocol, Port, Interface, Hostname, ServiceIp, Pid, Development) == (other.Protocol, other.Port, other.Interface, other.Hostname, other.ServiceIp, other.Pid, other.Development);

        public override int GetHashCode() =>
            HashCode.Combine(Protocol, Port, Interface, Hostname, ServiceIp, Pid, Development);
    }
}
