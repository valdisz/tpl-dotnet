namespace Sable
{
    using System.Threading.Tasks;
    using System.Threading;
    using App.Metrics.Health;

    public sealed class ManualHealthSwitch : HealthCheck, IManualHealthSwitch
    {
        public ManualHealthSwitch()
            : base("manual")
        {
        }

        public HealthState CurrentState { get; private set; }

        protected override ValueTask<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (CurrentState)
            {
                case HealthState.Degraded:
                    return new ValueTask<HealthCheckResult>(HealthCheckResult.Degraded());
                case HealthState.Unhealthy:
                    return new ValueTask<HealthCheckResult>(HealthCheckResult.Unhealthy());
                case HealthState.Healthy:
                default:
                    return new ValueTask<HealthCheckResult>(HealthCheckResult.Ignore());
            }
        }

        public void Degraded() => CurrentState = HealthState.Degraded;

        public void Healthy() => CurrentState = HealthState.Healthy;

        public void Unhealthy() => CurrentState = HealthState.Unhealthy;
    }
}
