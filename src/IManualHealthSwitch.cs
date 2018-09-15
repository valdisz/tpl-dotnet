namespace Sable
{
    public interface IManualHealthSwitch
    {
        HealthState CurrentState { get; }

        void Healthy();
        void Degraded();
        void Unhealthy();
    }
}
