namespace Swr.Net.Store;

public interface ISwrStore
{
    bool TryGet(string key, out CacheEntry? entry);
    void Set(string key, object? data);
    void Evict(string key);
    void InvalidateByPrefix(string prefix);
    void Clear();
}
