namespace tpl_dotnet
{
    public class ServiceOptions
    {
        public bool IsDevelopment { get; set; }
        public string[] Arguments { get; set; }
        public Serilog.ILogger FallbackLogger { get; set; }
    }
}
