using System;

namespace Sable
{
    public class NamingServiceOptions : IEquatable<NamingServiceOptions>
    {
        public string Name { get; set; }
        public string[] Tags { get; set; } = new string[0];
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan DeregisterTtl { get; set; } = TimeSpan.FromSeconds(600);
        public Uri Address { get; set; }

        public static bool operator ==(NamingServiceOptions left, NamingServiceOptions right) =>
            Equals(left, right);

        public static bool operator !=(NamingServiceOptions left, NamingServiceOptions right) =>
            !Equals(left, right);

        public override bool Equals(object obj) =>
            (obj is NamingServiceOptions metrics) && Equals(metrics);

        public bool Equals(NamingServiceOptions other) =>
            (Name, Tags, CheckInterval, DeregisterTtl, Address) == (other.Name, other.Tags, other.CheckInterval, other.DeregisterTtl, other.Address);

        public override int GetHashCode() =>
            HashCode.Combine(Name, Tags, CheckInterval, DeregisterTtl, Address);
    }
}
