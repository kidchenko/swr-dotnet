namespace Swr.Net.Store;

/// <summary>
/// Abstraction for the SWR cache storage backend. Implement this to use a custom store
/// (e.g., Redis via <c>IDistributedCache</c>). The default implementation is an in-memory
/// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>-based store.
/// </summary>
public interface ISwrStore
{
    /// <summary>
    /// Attempts to retrieve a cache entry by key. Returns true if found.
    /// </summary>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="entry">The cache entry if found, or null if not present.</param>
    /// <returns>True if a cache entry exists for the given key; otherwise false.</returns>
    bool TryGet(string key, out CacheEntry? entry);

    /// <summary>
    /// Stores or overwrites a cache entry with the current timestamp.
    /// </summary>
    /// <param name="key">The cache key to store under.</param>
    /// <param name="data">The data object to cache.</param>
    void Set(string key, object? data);

    /// <summary>
    /// Removes a single cache entry by exact key.
    /// </summary>
    /// <param name="key">The exact cache key to remove.</param>
    void Evict(string key);

    /// <summary>
    /// Removes all cache entries whose keys start with the given prefix.
    /// </summary>
    /// <param name="prefix">The key prefix to match. All entries with keys starting with this prefix are removed.</param>
    void InvalidateByPrefix(string prefix);

    /// <summary>
    /// Removes all cache entries from the store.
    /// </summary>
    void Clear();
}
