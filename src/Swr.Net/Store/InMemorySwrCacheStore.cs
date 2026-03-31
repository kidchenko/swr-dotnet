using System.Collections.Concurrent;

namespace Swr.Net.Store;

internal sealed class InMemorySwrCacheStore : ISwrStore
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeProvider _timeProvider;

    public InMemorySwrCacheStore(TimeProvider? timeProvider = null)
        => _timeProvider = timeProvider ?? TimeProvider.System;

    public bool TryGet(string key, out CacheEntry? entry)
        => _cache.TryGetValue(key, out entry);

    public void Set(string key, object? data)
        => _cache[key] = new CacheEntry(data, _timeProvider.GetUtcNow());

    public void Evict(string key)
        => _cache.TryRemove(key, out _);

    public void InvalidateByPrefix(string prefix)
    {
        var keysToRemove = _cache.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
            _cache.TryRemove(key, out _);
    }

    public void Clear() => _cache.Clear();
}
