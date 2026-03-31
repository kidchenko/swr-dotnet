using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class ErrorHandlingTests
{
    [Fact]
    public async Task CacheMiss_Fetch_Error_Sets_Error_On_Result()
    {
        var opts = new SwrOptions { RetryCount = 0 };
        var (cache, _, _, _) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) => throw new HttpRequestException("Network error"));

        var result = cache.Get<string>("api/data");
        await result.Completed.WaitAsync(TimeSpan.FromSeconds(5));

        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task CacheMiss_Fetch_Error_Sets_IsLoading_False()
    {
        var opts = new SwrOptions { RetryCount = 0 };
        var (cache, _, _, _) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) => throw new HttpRequestException("Network error"));

        var result = cache.Get<string>("api/data");
        await result.Completed.WaitAsync(TimeSpan.FromSeconds(5));

        result.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task Stale_Revalidation_Error_Preserves_Stale_Data()
    {
        var opts = new SwrOptions { RetryCount = 0 };
        var (cache, _, store, time) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) => throw new HttpRequestException("Network error"));

        // Pre-populate with stale data
        store.Set("api/data", "stale-data");
        time.Advance(TimeSpan.FromMinutes(3));

        var revalidated = new TaskCompletionSource();
        var result = cache.Get<string>("api/data", onRevalidated: _ => revalidated.TrySetResult());

        await revalidated.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // REQ-03: Stale data preserved on background revalidation error
        result.Data.Should().Be("stale-data");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Stale_Revalidation_Error_Sets_IsLoading_False()
    {
        var opts = new SwrOptions { RetryCount = 0 };
        var (cache, _, store, time) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) => throw new HttpRequestException("Network error"));

        store.Set("api/data", "stale-data");
        time.Advance(TimeSpan.FromMinutes(3));

        var revalidated = new TaskCompletionSource();
        var result = cache.Get<string>("api/data", onRevalidated: _ => revalidated.TrySetResult());

        await revalidated.Task.WaitAsync(TimeSpan.FromSeconds(5));

        result.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task CacheMiss_Fetch_Error_Does_Not_Store_In_Cache()
    {
        var opts = new SwrOptions { RetryCount = 0 };
        var (cache, _, store, _) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) => throw new HttpRequestException("Network error"));

        var result = cache.Get<string>("api/data");
        await result.Completed.WaitAsync(TimeSpan.FromSeconds(5));

        store.TryGet("api/data", out _).Should().BeFalse();
    }
}
