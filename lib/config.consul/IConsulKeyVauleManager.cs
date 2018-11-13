namespace Sable
{
    public interface IConsulKeyVauleManager
    {
        string Prefix { get; }

        bool ShouldLoad(string key);

        string MapKey(string key);

        string DecodeValue(byte[] value);
    }
}
