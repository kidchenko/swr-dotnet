using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class CacheFreshTests
{
    private static (Swr cache, MockHttpMessageHandler handler, global::Swr.Net.Store.InMemorySwrCacheStore store, Microsoft.Extensions.Time.Testing.FakeTimeProvider time) CreateWithFreshEntry(string key = "api/data", string value = "cached-value")
    {
        var tuple = SwrCacheFactory.Create();
        var (cache, handler, store, time) = tuple;
        store.Set(key, value);
        // Fresh: within StaleTime (default 2 min) — no time advance needed
        return tuple;
    }

    [Fact]
    public void Get_FreshHit_Returns_Cached_Data_Immediately()
    {
        var (cache, _, _, _) = CreateWithFreshEntry();

        var result = cache.Get<string>("api/data");

        result.Data.Should().Be("cached-value");
    }

    [Fact]
    public void Get_FreshHit_Sets_IsFromCache_True()
    {
        var (cache, _, _, _) = CreateWithFreshEntry();

        var result = cache.Get<string>("api/data");

        result.IsFromCache.Should().BeTrue();
    }

    [Fact]
    public void Get_FreshHit_Does_Not_Make_Http_Call()
    {
        var (cache, handler, _, _) = CreateWithFreshEntry();

        cache.Get<string>("api/data");

        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public void Get_FreshHit_Sets_IsLoading_False()
    {
        var (cache, _, _, _) = CreateWithFreshEntry();

        var result = cache.Get<string>("api/data");

        result.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void Get_FreshHit_Completed_Is_Already_Resolved()
    {
        var (cache, _, _, _) = CreateWithFreshEntry();

        var result = cache.Get<string>("api/data");

        result.Completed.IsCompleted.Should().BeTrue();
    }
}
