using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class CacheMissTests
{
    [Fact]
    public void Get_CacheMiss_Sets_IsLoading_True_Immediately()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create();

        var result = cache.Get<string>("api/data");

        result.IsLoading.Should().BeTrue();
    }

    [Fact]
    public async Task Get_CacheMiss_Fetches_And_Populates_Data()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("hello")));

        var result = cache.Get<string>("api/data");
        await result.Completed;

        result.Data.Should().Be("hello");
    }

    [Fact]
    public async Task Get_CacheMiss_Sets_IsFromCache_False()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("hello")));

        var result = cache.Get<string>("api/data");
        await result.Completed;

        result.IsFromCache.Should().BeFalse();
    }

    [Fact]
    public async Task Get_CacheMiss_Fires_OnRevalidated_After_Fetch()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("hello")));

        var revalidated = new TaskCompletionSource();
        var result = cache.Get<string>("api/data", onRevalidated: _ => revalidated.TrySetResult());
        await revalidated.Task.WaitAsync(TimeSpan.FromSeconds(5));

        revalidated.Task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Get_CacheMiss_Stores_Data_In_Cache()
    {
        var (cache, _, store, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("hello")));

        var result = cache.Get<string>("api/data");
        await result.Completed;

        store.TryGet("api/data", out var entry).Should().BeTrue();
        entry.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_CacheMiss_Sets_IsLoading_False_After_Fetch()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("world")));

        var result = cache.Get<string>("api/data");
        await result.Completed;

        result.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ExpiredEntry_Treats_As_CacheMiss()
    {
        var (cache, handler, store, time) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("fresh")));

        // Pre-populate cache
        store.Set("api/data", "stale");

        // Advance time past CacheTime (default 10 min)
        time.Advance(TimeSpan.FromMinutes(11));

        var result = cache.Get<string>("api/data");
        await result.Completed;

        handler.CallCount.Should().Be(1);
        result.Data.Should().Be("fresh");
    }
}
