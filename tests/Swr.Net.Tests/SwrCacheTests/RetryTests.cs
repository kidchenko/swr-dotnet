using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class RetryTests
{
    [Fact]
    public async Task Fetch_Retries_On_Failure_And_Succeeds()
    {
        int callCount = 0;
        var opts = new SwrOptions { RetryCount = 3, RetryBaseDelay = TimeSpan.FromMilliseconds(1) };

        var (cache, handler, _, _) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) =>
            {
                var count = Interlocked.Increment(ref callCount);
                if (count <= 2)
                    throw new HttpRequestException("Simulated failure");
                return Task.FromResult(HttpResponseHelper.JsonResponse("success"));
            });

        var result = cache.Get<string>("api/data");
        await result.Completed.WaitAsync(TimeSpan.FromSeconds(10));

        callCount.Should().Be(3);
        result.Data.Should().Be("success");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Fetch_Exhausts_Retries_Then_Sets_Error()
    {
        int callCount = 0;
        var opts = new SwrOptions { RetryCount = 2, RetryBaseDelay = TimeSpan.FromMilliseconds(1) };

        var (cache, handler, _, _) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) =>
            {
                Interlocked.Increment(ref callCount);
                throw new HttpRequestException("Always fails");
            });

        var result = cache.Get<string>("api/data");
        await result.Completed.WaitAsync(TimeSpan.FromSeconds(10));

        // Loop runs attempt 0..retryCount inclusive (3 iterations).
        // catch when (attempt < retryCount) catches attempts 0, 1 (retries).
        // attempt 2: exception not caught by when filter — propagates (no final call outside loop).
        // Total = retryCount + 1 = 3
        callCount.Should().Be(3);
        result.Error.Should().NotBeNull();
        result.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task Fetch_With_Single_Retry_Makes_Two_Calls_On_Failure()
    {
        int callCount = 0;
        var opts = new SwrOptions { RetryCount = 1, RetryBaseDelay = TimeSpan.FromMilliseconds(1) };

        var (cache, _, _, _) = SwrCacheFactory.Create(
            options: opts,
            handlerFunc: (req, ct) =>
            {
                Interlocked.Increment(ref callCount);
                throw new HttpRequestException("Always fails");
            });

        var result = cache.Get<string>("api/data");
        await result.Completed.WaitAsync(TimeSpan.FromSeconds(10));

        // Loop: attempt 0 (caught, retries), attempt 1 (not caught — propagates).
        // Total = retryCount + 1 = 2
        callCount.Should().Be(2);
        result.Error.Should().NotBeNull();
    }
}
