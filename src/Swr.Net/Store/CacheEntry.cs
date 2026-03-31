namespace Swr.Net.Store;

/// <summary>
/// Represents a cached data entry with its storage timestamp. Used by <see cref="ISwrStore"/>
/// implementations to track cache age.
/// </summary>
/// <param name="Data">The cached data object.</param>
/// <param name="StoredAt">The UTC timestamp when this entry was stored.</param>
public sealed record CacheEntry(object? Data, DateTimeOffset StoredAt);
