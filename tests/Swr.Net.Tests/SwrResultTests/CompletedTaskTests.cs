using FluentAssertions;
using Swr.Net;
using Xunit;

namespace Swr.Net.Tests.SwrResultTests;

public class CompletedTaskTests
{
    [Fact]
    public void Completed_Is_Not_Completed_Initially()
    {
        var result = new SwrResult<string>();

        result.Completed.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void SetCompleted_Resolves_Completed_Task()
    {
        var result = new SwrResult<string>();

        result.SetCompleted();

        result.Completed.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void NotifyRevalidated_Also_Resolves_Completed_Task()
    {
        var result = new SwrResult<string>();

        result.NotifyRevalidated();

        result.Completed.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void SetCompleted_Is_Idempotent()
    {
        var result = new SwrResult<string>();

        result.SetCompleted();
        var act = () => result.SetCompleted();

        act.Should().NotThrow();
        result.Completed.IsCompleted.Should().BeTrue();
    }
}
