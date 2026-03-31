namespace Swr.Net;

/// <summary>
/// Configuration options for SWR caching behavior. Can be set globally via DI or overridden per request.
/// </summary>
public record SwrOptions
{
    /// <summary>
    /// Duration after which cached data is considered stale. Stale data is returned immediately
    /// but triggers background revalidation. Default: 2 minutes.
    /// </summary>
    public TimeSpan StaleTime { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Duration after which cached data is evicted entirely. A new network request is required.
    /// Default: 10 minutes.
    /// </summary>
    public TimeSpan CacheTime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// When true, concurrent requests for the same cache key share a single network request.
    /// Default: true.
    /// </summary>
    public bool DeduplicateRequests { get; set; } = true;

    /// <summary>
    /// Number of retry attempts on fetch failure. Uses exponential backoff. Default: 3.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Base delay between retry attempts. Actual delay doubles with each attempt. Default: 500ms.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
