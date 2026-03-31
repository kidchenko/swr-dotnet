using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class InvalidateTests
{
    [Fact]
    public async Task Invalidate_Removes_Exact_Key()
    {
        var (cache, _, store, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("value")));

        // Populate both keys
        await cache.GetAsync<string>("api/users/1");
        await cache.GetAsync<string>("api/users/2");

        // Invalidate only one
        cache.Invalidate("api/users/1");

        // api/users/1 should be a miss
        var result1 = cache.Get<string>("api/users/1");
        result1.IsLoading.Should().BeTrue();
        result1.IsFromCache.Should().BeFalse();

        // api/users/2 should still be a hit
        var result2 = cache.Get<string>("api/users/2");
        result2.IsFromCache.Should().BeTrue();
        result2.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task Invalidate_Normalizes_Key()
    {
        var (cache, handler, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("value")));

        // Populate with query params that will be normalized
        await cache.GetAsync<string>("api/data?b=2&a=1");
        var callCountAfterPrime = handler.CallCount;

        // Invalidate using the non-normalized form
        cache.Invalidate("api/data?b=2&a=1");

        // Should be a cache miss
        var result = cache.Get<string>("api/data?b=2&a=1");
        result.IsLoading.Should().BeTrue();
        result.IsFromCache.Should().BeFalse();

        await result.Completed;
        handler.CallCount.Should().BeGreaterThan(callCountAfterPrime);
    }

    [Fact]
    public async Task InvalidateByPrefix_Removes_Matching_Entries()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("value")));

        // Populate multiple keys
        await cache.GetAsync<string>("api/users/1");
        await cache.GetAsync<string>("api/users/2");
        await cache.GetAsync<string>("api/posts/1");

        // Invalidate by prefix
        cache.InvalidateByPrefix("api/users");

        // Users should be misses
        var users1 = cache.Get<string>("api/users/1");
        users1.IsLoading.Should().BeTrue();
        users1.IsFromCache.Should().BeFalse();

        var users2 = cache.Get<string>("api/users/2");
        users2.IsLoading.Should().BeTrue();
        users2.IsFromCache.Should().BeFalse();

        // Posts should still be a hit
        var posts1 = cache.Get<string>("api/posts/1");
        posts1.IsFromCache.Should().BeTrue();
        posts1.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateByPrefix_No_Match_Does_Nothing()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("value")));

        // Populate a key
        await cache.GetAsync<string>("api/users/1");

        // Invalidate with a non-matching prefix
        cache.InvalidateByPrefix("api/products");

        // Entry should still be a hit
        var result = cache.Get<string>("api/users/1");
        result.IsFromCache.Should().BeTrue();
        result.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateAll_Clears_Entire_Cache()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("value")));

        // Populate multiple keys
        await cache.GetAsync<string>("api/users");
        await cache.GetAsync<string>("api/posts");
        await cache.GetAsync<string>("api/comments");

        // Invalidate everything
        cache.InvalidateAll();

        // All should be misses
        var users = cache.Get<string>("api/users");
        users.IsLoading.Should().BeTrue();
        users.IsFromCache.Should().BeFalse();

        var posts = cache.Get<string>("api/posts");
        posts.IsLoading.Should().BeTrue();
        posts.IsFromCache.Should().BeFalse();

        var comments = cache.Get<string>("api/comments");
        comments.IsLoading.Should().BeTrue();
        comments.IsFromCache.Should().BeFalse();
    }
}
