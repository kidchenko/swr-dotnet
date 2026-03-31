using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class DeduplicationTests
{
    [Fact]
    public async Task Concurrent_Gets_Same_Key_Produce_Single_Http_Call()
    {
        var requestReceived = new TaskCompletionSource();
        var releaseRequest = new TaskCompletionSource();

        var (cache, handler, _, _) = SwrCacheFactory.Create(
            handlerFunc: async (req, ct) =>
            {
                requestReceived.TrySetResult();
                await releaseRequest.Task;
                return HttpResponseHelper.JsonResponse("data");
            });

        // Launch 5 concurrent Gets for the same key
        var results = Enumerable.Range(0, 5)
            .Select(_ => cache.Get<string>("api/shared"))
            .ToList();

        // Wait for at least one request to be in-flight
        await requestReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Release the HTTP response
        releaseRequest.SetResult();

        // Wait for all to complete
        await Task.WhenAll(results.Select(r => r.Completed));

        handler.CallCount.Should().Be(1);
        results.Should().AllSatisfy(r => r.Data.Should().Be("data"));
    }

    [Fact]
    public async Task Sequential_Gets_After_Cache_Expiry_Make_Separate_Calls()
    {
        var (cache, handler, store, time) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("data")));

        // First Get (cache miss)
        var result1 = cache.Get<string>("api/data");
        await result1.Completed;

        handler.CallCount.Should().Be(1);

        // Advance time past CacheTime to expire cache
        time.Advance(TimeSpan.FromMinutes(11));

        // Second Get (expired — treated as cache miss)
        var result2 = cache.Get<string>("api/data");
        await result2.Completed;

        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task Deduplication_Disabled_Makes_Separate_Calls()
    {
        var requestGate = new SemaphoreSlim(0);
        var releaseGate = new TaskCompletionSource();
        int callCount = 0;

        var opts = new SwrOptions { DeduplicateRequests = false };
        var (cache, handler, _, _) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: async (req, ct) =>
            {
                Interlocked.Increment(ref callCount);
                requestGate.Release();
                await releaseGate.Task;
                return HttpResponseHelper.JsonResponse("data");
            });

        var r1 = cache.Get<string>("api/data");
        var r2 = cache.Get<string>("api/data");

        // Wait for both requests to be in-flight
        await requestGate.WaitAsync(TimeSpan.FromSeconds(5));
        await requestGate.WaitAsync(TimeSpan.FromSeconds(5));

        releaseGate.SetResult();

        await Task.WhenAll(r1.Completed, r2.Completed);

        callCount.Should().Be(2);
    }
}
