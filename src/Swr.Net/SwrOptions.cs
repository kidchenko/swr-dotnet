namespace Swr.Net;

public record SwrOptions
{
    public TimeSpan StaleTime { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan CacheTime { get; set; } = TimeSpan.FromMinutes(10);
    public bool DeduplicateRequests { get; set; } = true;
    public int RetryCount { get; set; } = 3;
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
