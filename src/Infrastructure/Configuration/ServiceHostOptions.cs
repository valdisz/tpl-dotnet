namespace Sable
{
    public class ServiceHostOptions
    {
        public string[] Arguments { get; set; }
        public Serilog.ILogger FallbackLogger { get; set; }
    }
}
