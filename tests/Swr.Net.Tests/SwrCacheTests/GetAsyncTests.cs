using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class GetAsyncTests
{
    [Fact]
    public async Task GetAsync_Awaits_Until_Data_Available_On_CacheMiss()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("async-data")));

        var result = await cache.GetAsync<string>("api/data");

        result.Data.Should().Be("async-data");
        result.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_Returns_Immediately_On_FreshHit()
    {
        var (cache, handler, store, _) = SwrCacheFactory.Create();

        store.Set("api/data", "cached-async");

        var result = await cache.GetAsync<string>("api/data");

        result.Data.Should().Be("cached-async");
        result.IsFromCache.Should().BeTrue();
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_Returns_Stale_Data_Without_Waiting_For_Revalidation()
    {
        // Handler blocks forever to verify GetAsync returns immediately with stale data
        var blockForever = new TaskCompletionSource<HttpResponseMessage>();
        var (cache, _, store, time) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => blockForever.Task);

        store.Set("api/data", "stale-data");
        time.Advance(TimeSpan.FromMinutes(3));

        // GetAsync should return immediately (Completed resolves at stale path per Pitfall #4)
        var resultTask = cache.GetAsync<string>("api/data");
        var completed = await Task.WhenAny(resultTask, Task.Delay(1000));

        completed.Should().BeSameAs(resultTask, "GetAsync should return stale data without waiting for revalidation");
        var result = await resultTask;
        result.Data.Should().Be("stale-data");

        // Cleanup: unblock background task
        blockForever.TrySetCanceled();
    }

    [Fact]
    public async Task GetAsync_Returns_SwrResult_With_Populated_Data()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("result-data")));

        var result = await cache.GetAsync<string>("api/data");

        result.Should().NotBeNull();
        result.Data.Should().Be("result-data");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_On_Error_Returns_Result_With_Error_Set()
    {
        var opts = new SwrOptions { RetryCount = 0 };
        var (cache, _, _, _) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) => throw new HttpRequestException("Async error"));

        var result = await cache.GetAsync<string>("api/data");

        result.Error.Should().Contain("Async error");
        result.IsLoading.Should().BeFalse();
    }
}
