namespace Sable
{
    public interface IRuntimeConfiguration
    {
        void Set(string key, string value);
        bool TryGet(string key, out string value);
    }
}
