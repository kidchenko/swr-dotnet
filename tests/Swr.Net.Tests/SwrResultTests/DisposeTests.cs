using FluentAssertions;
using Swr.Net;
using Xunit;

namespace Swr.Net.Tests.SwrResultTests;

public class DisposeTests
{
    [Fact]
    public void Dispose_Nulls_OnRevalidated()
    {
        var result = new SwrResult<string>();
        var handlerCalled = false;
        result.OnRevalidated += () => handlerCalled = true;

        result.Dispose();
        result.NotifyRevalidated();

        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public void Dispose_Cancels_Completed_Task_If_Not_Yet_Completed()
    {
        var result = new SwrResult<string>();

        result.Dispose();

        result.Completed.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public void Dispose_After_SetCompleted_Leaves_Task_Completed()
    {
        var result = new SwrResult<string>();

        result.SetCompleted();
        result.Dispose();

        result.Completed.IsCompleted.Should().BeTrue();
        result.Completed.IsCanceled.Should().BeFalse();
    }
}
