namespace Sable
{
    public class ServiceHostOptions
    {
        public bool IsDevelopment { get; set; }
        public string[] Arguments { get; set; }
        public Serilog.ILogger FallbackLogger { get; set; }
    }
}
