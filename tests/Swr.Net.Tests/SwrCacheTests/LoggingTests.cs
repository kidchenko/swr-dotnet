using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swr.Net.Store;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

internal sealed class FakeLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}

internal sealed class SingleClientFactoryForTest : IHttpClientFactory
{
    private readonly HttpClient _client;
    public SingleClientFactoryForTest(HttpClient client) => _client = client;
    public HttpClient CreateClient(string name) => _client;
}

public class LoggingTests
{
    private static (Swr cache, MockHttpMessageHandler handler, InMemorySwrCacheStore store, Microsoft.Extensions.Time.Testing.FakeTimeProvider time, FakeLogger<Swr> logger) CreateWithLogger(
        SwrOptions? options = null,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handlerFunc = null)
    {
        var time = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemorySwrCacheStore(time);
        var handler = new MockHttpMessageHandler(handlerFunc ?? ((req, ct) =>
            Task.FromResult(HttpResponseHelper.JsonResponse("default"))));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://test.local") };
        var logger = new FakeLogger<Swr>();
        var cache = new Swr(new SingleClientFactoryForTest(http), store,
            Options.Create(options ?? new SwrOptions()), logger, time);
        return (cache, handler, store, time, logger);
    }

    [Fact]
    public async Task CacheMiss_LogsDebugWithEventId1001()
    {
        var (cache, _, _, _, logger) = CreateWithLogger();

        await cache.GetAsync<string>("api/data");

        logger.Entries.Should().Contain(e =>
            e.EventId.Id == 1001 &&
            e.Level == LogLevel.Debug &&
            e.Message.Contains("MISS"));
    }

    [Fact]
    public async Task CacheFresh_LogsDebugWithEventId1003()
    {
        var (cache, _, _, _, logger) = CreateWithLogger(
            options: new SwrOptions { StaleTime = TimeSpan.FromMinutes(5) });

        // First call — populate cache
        await cache.GetAsync<string>("api/data");

        // Clear entries so we only look at second call
        logger.Entries.Clear();

        // Second call — should be FRESH (within StaleTime)
        await cache.GetAsync<string>("api/data");

        logger.Entries.Should().Contain(e =>
            e.EventId.Id == 1003 &&
            e.Level == LogLevel.Debug &&
            e.Message.Contains("FRESH"));
    }

    [Fact]
    public async Task CacheStale_LogsDebugWithEventId1002()
    {
        var staleOptions = new SwrOptions
        {
            StaleTime = TimeSpan.FromMinutes(1),
            CacheTime = TimeSpan.FromMinutes(10)
        };
        var (cache, _, _, time, logger) = CreateWithLogger(options: staleOptions);

        // Populate cache
        await cache.GetAsync<string>("api/data");

        // Advance past StaleTime
        time.Advance(TimeSpan.FromMinutes(2));
        logger.Entries.Clear();

        // This should return stale and trigger background revalidation
        var result = cache.Get<string>("api/data");
        await result.Completed;

        logger.Entries.Should().Contain(e =>
            e.EventId.Id == 1002 &&
            e.Level == LogLevel.Debug &&
            e.Message.Contains("STALE"));
    }

    [Fact]
    public async Task RevalidationComplete_LogsInformationWithEventId1004()
    {
        var staleOptions = new SwrOptions
        {
            StaleTime = TimeSpan.FromMinutes(1),
            CacheTime = TimeSpan.FromMinutes(10)
        };
        var (cache, _, _, time, logger) = CreateWithLogger(options: staleOptions);

        // Populate cache
        await cache.GetAsync<string>("api/data");

        // Advance past StaleTime
        time.Advance(TimeSpan.FromMinutes(2));

        // Trigger stale path — background revalidation fires
        var tcs = new TaskCompletionSource();
        var result = cache.Get<string>("api/data", onRevalidated: _ => tcs.TrySetResult());
        await result.Completed;

        // Wait for background revalidation to complete
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        logger.Entries.Should().Contain(e =>
            e.EventId.Id == 1004 &&
            e.Level == LogLevel.Information);
    }

    [Fact]
    public async Task FetchRetry_LogsWarningWithEventId1005()
    {
        var callCount = 0;
        var (cache, _, _, _, logger) = CreateWithLogger(
            options: new SwrOptions { RetryCount = 1, RetryBaseDelay = TimeSpan.FromMilliseconds(1) },
            handlerFunc: (req, ct) =>
            {
                var count = Interlocked.Increment(ref callCount);
                if (count == 1)
                    throw new HttpRequestException("Simulated failure");
                return Task.FromResult(HttpResponseHelper.JsonResponse("success"));
            });

        await cache.GetAsync<string>("api/data");

        logger.Entries.Should().Contain(e =>
            e.EventId.Id == 1005 &&
            e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task FetchFailed_LogsErrorWithEventId1007()
    {
        var (cache, _, _, _, logger) = CreateWithLogger(
            options: new SwrOptions { RetryCount = 0, RetryBaseDelay = TimeSpan.FromMilliseconds(1) },
            handlerFunc: (req, ct) =>
                throw new HttpRequestException("Always fails"));

        var result = await cache.GetAsync<string>("api/data");

        // The fetch fails so result.Error should be set
        logger.Entries.Should().Contain(e =>
            e.EventId.Id == 1007 &&
            e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task BackgroundRevalidationFailed_LogsWarningWithEventId1006()
    {
        var callCount = 0;
        var staleOptions = new SwrOptions
        {
            StaleTime = TimeSpan.FromMinutes(1),
            CacheTime = TimeSpan.FromMinutes(10),
            RetryCount = 0
        };
        var (cache, _, _, time, logger) = CreateWithLogger(
            options: staleOptions,
            handlerFunc: (req, ct) =>
            {
                var count = Interlocked.Increment(ref callCount);
                // First call succeeds (populate cache), subsequent fail (background revalidation)
                if (count == 1)
                    return Task.FromResult(HttpResponseHelper.JsonResponse("initial"));
                throw new HttpRequestException("Simulated revalidation failure");
            });

        // Populate cache
        await cache.GetAsync<string>("api/data");

        // Advance past StaleTime
        time.Advance(TimeSpan.FromMinutes(2));

        // Trigger stale path — background revalidation fires and fails
        var tcs = new TaskCompletionSource();
        var result = cache.Get<string>("api/data", onRevalidated: _ => tcs.TrySetResult());
        await result.Completed;

        // Wait for background revalidation to complete (fail)
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        logger.Entries.Should().Contain(e =>
            e.EventId.Id == 1006 &&
            e.Level == LogLevel.Warning);
    }
}
