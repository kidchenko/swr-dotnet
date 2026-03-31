namespace Swr.Net.Keys;

/// <summary>
/// Utilities for normalizing and constructing SWR cache keys.
/// </summary>
public static class SwrCacheKey
{
    /// <summary>
    /// Normalizes a URL cache key by sorting query string parameters alphabetically.
    /// Ensures <c>/api/data?b=2&amp;a=1</c> and <c>/api/data?a=1&amp;b=2</c> resolve to the same cache entry.
    /// </summary>
    /// <param name="url">The URL to normalize.</param>
    /// <returns>The normalized URL with query parameters sorted alphabetically.</returns>
    public static string Normalize(string url)
    {
        var queryIndex = url.IndexOf('?');
        if (queryIndex < 0) return url;

        var path = url[..queryIndex];
        var query = url[(queryIndex + 1)..];
        var parts = query.Split('&');
        Array.Sort(parts, StringComparer.Ordinal);
        return $"{path}?{string.Join('&', parts)}";
    }

    /// <summary>
    /// Constructs a composite cache key from a URL and additional context parts.
    /// Parts are joined with a <c>::</c> separator. Use for same-URL-different-context scenarios
    /// (e.g., per-user or per-tenant caching).
    /// </summary>
    /// <param name="url">The base URL, which will be normalized before combining.</param>
    /// <param name="context">Additional context parts to append to the key.</param>
    /// <returns>A composite cache key combining the normalized URL and all context parts.</returns>
    public static string From(string url, params string[] context)
    {
        var normalized = Normalize(url);
        if (context.Length == 0) return normalized;
        return $"{normalized}::{string.Join("::", context)}";
    }
}
