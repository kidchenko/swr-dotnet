using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Swr.Net.Store;
using Xunit;

namespace Swr.Net.Tests.InMemorySwrCacheStoreTests;

public class TryGetSetTests
{
    [Fact]
    public void TryGet_Returns_False_For_Missing_Key()
    {
        var store = new InMemorySwrCacheStore();

        var result = store.TryGet("missing", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void Set_Then_TryGet_Returns_Entry()
    {
        var store = new InMemorySwrCacheStore();

        store.Set("k", "v");
        var result = store.TryGet("k", out var entry);

        result.Should().BeTrue();
        entry!.Data.Should().Be("v");
    }

    [Fact]
    public void Set_Records_StoredAt_From_TimeProvider()
    {
        var fakeTime = new FakeTimeProvider();
        var expectedTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        fakeTime.SetUtcNow(expectedTime);
        var store = new InMemorySwrCacheStore(fakeTime);

        store.Set("k", "v");
        store.TryGet("k", out var entry);

        entry!.StoredAt.Should().Be(expectedTime);
    }

    [Fact]
    public void Set_Overwrites_Existing_Entry()
    {
        var store = new InMemorySwrCacheStore();

        store.Set("k", "first");
        store.Set("k", "second");
        store.TryGet("k", out var entry);

        entry!.Data.Should().Be("second");
    }
}
