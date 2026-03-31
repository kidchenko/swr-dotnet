using FluentAssertions;
using Swr.Net.Tests.Helpers;
using Xunit;

namespace Swr.Net.Tests.SwrCacheTests;

public class MutateTests
{
    [Fact]
    public async Task MutateAsync_Executes_Action_Then_Invalidates_Key()
    {
        var (cache, handler, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("value")));

        // Populate cache
        var initial = await cache.GetAsync<string>("api/users");
        initial.Data.Should().Be("value");
        var callCountAfterPrime = handler.CallCount;

        // Mutate and invalidate
        await cache.MutateAsync(async () => await Task.CompletedTask, "api/users");

        // Next Get should be a cache miss
        var result = cache.Get<string>("api/users");
        result.IsLoading.Should().BeTrue();
        result.IsFromCache.Should().BeFalse();

        await result.Completed;
        handler.CallCount.Should().BeGreaterThan(callCountAfterPrime);
    }

    [Fact]
    public async Task MutateAsync_Invalidates_Multiple_Keys()
    {
        var (cache, handler, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("value")));

        // Populate both keys
        await cache.GetAsync<string>("api/users");
        await cache.GetAsync<string>("api/posts");
        var callCountAfterPrime = handler.CallCount;

        // Mutate invalidating both
        await cache.MutateAsync(async () => await Task.CompletedTask, "api/users", "api/posts");

        // Both should be cache misses
        var usersResult = cache.Get<string>("api/users");
        var postsResult = cache.Get<string>("api/posts");

        usersResult.IsLoading.Should().BeTrue();
        usersResult.IsFromCache.Should().BeFalse();
        postsResult.IsLoading.Should().BeTrue();
        postsResult.IsFromCache.Should().BeFalse();

        await Task.WhenAll(usersResult.Completed, postsResult.Completed);
        handler.CallCount.Should().BeGreaterThan(callCountAfterPrime);
    }

    [Fact]
    public async Task MutateAsync_Does_Not_Invalidate_On_Mutation_Failure()
    {
        var (cache, _, store, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("cached")));

        // Populate cache
        await cache.GetAsync<string>("api/users");
        store.TryGet("api/users", out var entry).Should().BeTrue();

        // Mutate with a failing action
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cache.MutateAsync(() => throw new InvalidOperationException("fail"), "api/users"));

        // Cache entry should still be there (NOT invalidated on failure)
        var result = cache.Get<string>("api/users");
        result.IsFromCache.Should().BeTrue();
        result.IsLoading.Should().BeFalse();
        result.Data.Should().Be("cached");
    }

    [Fact]
    public async Task MutateAsync_Normalizes_Keys_Before_Invalidation()
    {
        var (cache, handler, _, _) = SwrCacheFactory.Create(
            handlerFunc: (req, ct) => Task.FromResult(HttpResponseHelper.JsonResponse("value")));

        // Populate with query params that will be normalized
        await cache.GetAsync<string>("api/users?b=2&a=1");
        var callCountAfterPrime = handler.CallCount;

        // Invalidate using the non-normalized form
        await cache.MutateAsync(async () => await Task.CompletedTask, "api/users?b=2&a=1");

        // Should be a cache miss now
        var result = cache.Get<string>("api/users?b=2&a=1");
        result.IsLoading.Should().BeTrue();
        result.IsFromCache.Should().BeFalse();

        await result.Completed;
        handler.CallCount.Should().BeGreaterThan(callCountAfterPrime);
    }

    [Fact]
    public async Task MutateAsync_Executes_Actual_Mutation_Action()
    {
        var (cache, _, _, _) = SwrCacheFactory.Create();
        var mutationRan = false;

        await cache.MutateAsync(() =>
        {
            mutationRan = true;
            return Task.CompletedTask;
        }, "key");

        mutationRan.Should().BeTrue();
    }
}
