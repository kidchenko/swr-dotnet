namespace Swr.Net.Store;

public sealed record CacheEntry(object? Data, DateTimeOffset StoredAt);
