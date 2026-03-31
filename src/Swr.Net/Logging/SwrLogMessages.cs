using Microsoft.Extensions.Logging;

namespace Swr.Net.Logging;

internal static partial class SwrLogMessages
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug,
        Message = "Cache MISS for key '{Key}' — fetching from network")]
    internal static partial void LogCacheMiss(this ILogger logger, string key);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug,
        Message = "Cache STALE for key '{Key}' (age {Age}) — returning stale data, revalidating in background")]
    internal static partial void LogCacheStale(this ILogger logger, string key, TimeSpan age);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Debug,
        Message = "Cache FRESH for key '{Key}' — returning cached data")]
    internal static partial void LogCacheFresh(this ILogger logger, string key);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information,
        Message = "Revalidation complete for key '{Key}'")]
    internal static partial void LogRevalidationComplete(this ILogger logger, string key);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning,
        Message = "Fetch attempt {Attempt}/{Total} failed for key '{Key}': {Error}")]
    internal static partial void LogRetry(this ILogger logger, int attempt, int total, string key, string error);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Warning,
        Message = "Background revalidation failed for key '{Key}' — preserving stale data. Error: {Error}")]
    internal static partial void LogRevalidationFailed(this ILogger logger, string key, string error);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Error,
        Message = "Fetch failed for key '{Key}' after {Attempts} attempt(s): {Error}")]
    internal static partial void LogFetchFailed(this ILogger logger, string key, int attempts, string error);
}
