using FluentAssertions;
using Swr.Net.Store;
using Xunit;

namespace Swr.Net.Tests.InMemorySwrCacheStoreTests;

public class InvalidateTests
{
    [Fact]
    public void Evict_Removes_Entry()
    {
        var store = new InMemorySwrCacheStore();
        store.Set("key", "value");

        store.Evict("key");

        store.TryGet("key", out _).Should().BeFalse();
    }

    [Fact]
    public void Evict_NonExistent_Key_Does_Not_Throw()
    {
        var store = new InMemorySwrCacheStore();

        var act = () => store.Evict("missing");

        act.Should().NotThrow();
    }

    [Fact]
    public void InvalidateByPrefix_Removes_Matching_Keys()
    {
        var store = new InMemorySwrCacheStore();
        store.Set("api/users/1", "user1");
        store.Set("api/users/2", "user2");
        store.Set("api/products/1", "product1");

        store.InvalidateByPrefix("api/users");

        store.TryGet("api/users/1", out _).Should().BeFalse();
        store.TryGet("api/users/2", out _).Should().BeFalse();
        store.TryGet("api/products/1", out _).Should().BeTrue();
    }

    [Fact]
    public void InvalidateByPrefix_No_Match_Does_Nothing()
    {
        var store = new InMemorySwrCacheStore();
        store.Set("api/users/1", "user1");

        var act = () => store.InvalidateByPrefix("missing/");

        act.Should().NotThrow();
        store.TryGet("api/users/1", out _).Should().BeTrue();
    }

    [Fact]
    public void Clear_Removes_All_Entries()
    {
        var store = new InMemorySwrCacheStore();
        store.Set("key1", "v1");
        store.Set("key2", "v2");
        store.Set("key3", "v3");

        store.Clear();

        store.TryGet("key1", out _).Should().BeFalse();
        store.TryGet("key2", out _).Should().BeFalse();
        store.TryGet("key3", out _).Should().BeFalse();
    }
}
