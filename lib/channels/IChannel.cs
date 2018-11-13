namespace Sable
{
    public interface IChannelFactory<TChannel>
    {
        TChannel Create();
    }

    public interface IChannelStatus
    {
        bool Operational { get; }

        Task OpenAsync();

        Task CloseAsync();
    }

    public interface IChannel
    {

    }
}
