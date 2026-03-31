namespace Swr.Net;

public record SwrOptions
{
    public TimeSpan StaleTime { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan CacheTime { get; init; } = TimeSpan.FromMinutes(10);
    public bool DeduplicateRequests { get; init; } = true;
    public int RetryCount { get; init; } = 3;
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(500);
}
