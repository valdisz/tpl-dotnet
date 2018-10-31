namespace Sable
{
    using System;

    public interface IHealthEndpoint
    {
        string Name { get; }
        Uri RelativeUri { get; }
        (string name, string[] values)[] Headers { get; }
    }
}
