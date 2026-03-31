using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class PerRequestOptionsTests
{
    [Fact]
    public void Get_Uses_PerRequest_StaleTime_Over_Defaults()
    {
        // Default StaleTime = 2min, per-request StaleTime = 10min
        // Entry is 3min old — stale with defaults, fresh with per-request options
        var defaultOpts = new SwrOptions { StaleTime = TimeSpan.FromMinutes(2) };
        var (cache, handler, store, time) = SwrCacheFactory.Create(options: defaultOpts);

        store.Set("api/data", "cached-value");
        time.Advance(TimeSpan.FromMinutes(3));

        var perRequestOpts = new SwrOptions { StaleTime = TimeSpan.FromMinutes(10) };
        var result = cache.Get<string>("api/data", options: perRequestOpts);

        // With per-request options (StaleTime=10min), 3min-old entry is FRESH
        result.IsFromCache.Should().BeTrue();
        result.IsLoading.Should().BeFalse(); // Fresh, no background revalidation
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public void Get_Uses_Default_Options_When_None_Provided()
    {
        // Default StaleTime = 2min
        // Entry is 3min old — stale with defaults
        var defaultOpts = new SwrOptions { StaleTime = TimeSpan.FromMinutes(2) };
        var (cache, handler, store, time) = SwrCacheFactory.Create(options: defaultOpts);

        store.Set("api/data", "cached-value");
        time.Advance(TimeSpan.FromMinutes(3));

        var result = cache.Get<string>("api/data"); // no per-request options

        // With defaults (StaleTime=2min), 3min-old entry is STALE
        result.IsFromCache.Should().BeTrue();
        result.IsLoading.Should().BeTrue(); // Stale, background revalidation triggered
    }

    [Fact]
    public async Task Get_Uses_PerRequest_CacheTime_For_Expiry()
    {
        // Default CacheTime = 10min, per-request CacheTime = 1min
        // Entry is 2min old — valid with defaults, expired with per-request CacheTime=1min
        var defaultOpts = new SwrOptions { CacheTime = TimeSpan.FromMinutes(10) };
        var (cache, handler, store, time) = SwrCacheFactory.Create(
            options: defaultOpts,
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("fresh")));

        store.Set("api/data", "old-cached");
        time.Advance(TimeSpan.FromMinutes(2));

        var perRequestOpts = new SwrOptions { CacheTime = TimeSpan.FromMinutes(1) };
        var result = cache.Get<string>("api/data", options: perRequestOpts);
        await result.Completed.WaitAsync(TimeSpan.FromSeconds(5));

        // Expired with per-request opts — treated as MISS, fetched fresh
        handler.CallCount.Should().Be(1);
        result.Data.Should().Be("fresh");
    }
}
