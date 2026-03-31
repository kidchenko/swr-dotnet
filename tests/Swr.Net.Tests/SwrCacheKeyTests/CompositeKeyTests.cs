using FluentAssertions;
using Swr.Net.Keys;
using Xunit;

namespace Swr.Net.Tests.SwrCacheKeyTests;

public class CompositeKeyTests
{
    [Fact]
    public void From_Without_Context_Returns_Normalized_Url()
    {
        var result = SwrCacheKey.From("api/users?b=2&a=1");

        result.Should().Be("api/users?a=1&b=2");
    }

    [Fact]
    public void From_With_Single_Context_Appends_Separator()
    {
        var result = SwrCacheKey.From("api/users", "tenant-1");

        result.Should().Be("api/users::tenant-1");
    }

    [Fact]
    public void From_With_Multiple_Context_Joins_With_Separator()
    {
        var result = SwrCacheKey.From("api/users", "tenant-1", "role-admin");

        result.Should().Be("api/users::tenant-1::role-admin");
    }
}
