using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class CacheStaleTests
{
    private static (SwrCache cache, MockHttpMessageHandler handler, Swr.Net.Store.InMemorySwrCacheStore store, Microsoft.Extensions.Time.Testing.FakeTimeProvider time) CreateWithStaleEntry(
        string key = "api/data",
        string staleValue = "stale-value",
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handlerFunc = null)
    {
        var tuple = SwrCacheFactory.Create(handlerFunc: handlerFunc ??
            ((req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("fresh-value"))));
        var (cache, handler, store, time) = tuple;
        store.Set(key, staleValue);
        // Advance past StaleTime (2 min) but within CacheTime (10 min)
        time.Advance(TimeSpan.FromMinutes(3));
        return tuple;
    }

    [Fact]
    public void Get_StaleHit_Returns_Cached_Data_Immediately()
    {
        var (cache, _, _, _) = CreateWithStaleEntry();

        var result = cache.Get<string>("api/data");

        result.Data.Should().Be("stale-value");
    }

    [Fact]
    public void Get_StaleHit_Sets_IsFromCache_True_And_IsLoading_True()
    {
        var (cache, _, _, _) = CreateWithStaleEntry();

        var result = cache.Get<string>("api/data");

        result.IsFromCache.Should().BeTrue();
        result.IsLoading.Should().BeTrue();
    }

    [Fact]
    public async Task Get_StaleHit_Triggers_Background_Revalidation()
    {
        var (cache, handler, _, _) = CreateWithStaleEntry();

        var result = cache.Get<string>("api/data");

        // Wait for background revalidation to complete
        await Task.Delay(200);

        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_StaleHit_Updates_Data_After_Revalidation()
    {
        var (cache, _, _, _) = CreateWithStaleEntry();

        var revalidated = new TaskCompletionSource();
        var result = cache.Get<string>("api/data", onRevalidated: _ => revalidated.TrySetResult());

        await revalidated.Task.WaitAsync(TimeSpan.FromSeconds(5));

        result.Data.Should().Be("fresh-value");
    }

    [Fact]
    public void Get_StaleHit_Completed_Is_Resolved_Before_Revalidation()
    {
        var (cache, _, _, _) = CreateWithStaleEntry();

        var result = cache.Get<string>("api/data");

        // Completed resolves immediately (before background revalidation)
        result.Completed.IsCompleted.Should().BeTrue();
    }
}
