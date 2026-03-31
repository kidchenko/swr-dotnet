using FluentAssertions;
using Swr.Net.Keys;
using Xunit;

namespace Swr.Net.Tests.SwrCacheKeyTests;

public class NormalizeTests
{
    [Fact]
    public void Url_Without_Query_Returns_Unchanged()
    {
        var result = SwrCacheKey.Normalize("api/users");

        result.Should().Be("api/users");
    }

    [Fact]
    public void Url_With_Sorted_Query_Returns_Unchanged()
    {
        var result = SwrCacheKey.Normalize("api/users?a=1&b=2");

        result.Should().Be("api/users?a=1&b=2");
    }

    [Fact]
    public void Url_With_Unsorted_Query_Returns_Sorted()
    {
        var result = SwrCacheKey.Normalize("api/users?b=2&a=1");

        result.Should().Be("api/users?a=1&b=2");
    }

    [Fact]
    public void Url_With_Multiple_Params_Sorts_All()
    {
        var result = SwrCacheKey.Normalize("api?z=3&a=1&m=2");

        result.Should().Be("api?a=1&m=2&z=3");
    }
}
