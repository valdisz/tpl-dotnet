namespace Sable
{
    using clipr;

    [ApplicationInfo(Description = "Optional startup options.")]
    public class StartOptions
    {
        [NamedArgument('d', "dev",
            Constraint = NumArgsConstraint.Optional,
            Action = ParseAction.Store,
            Const = true)]
        public bool IsDevelopment { get; set; }
    }
}
