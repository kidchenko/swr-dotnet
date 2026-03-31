using System.ComponentModel;
using FluentAssertions;
using Swr.Net;
using Xunit;

namespace Swr.Net.Tests.SwrResultTests;

public class PropertyNotificationTests
{
    [Fact]
    public void Setting_Data_Raises_PropertyChanged()
    {
        var result = new SwrResult<string>();
        string? capturedPropertyName = null;
        result.PropertyChanged += (_, e) => capturedPropertyName = e.PropertyName;

        result.Data = "hello";

        capturedPropertyName.Should().Be("Data");
    }

    [Fact]
    public void Setting_IsLoading_Raises_PropertyChanged()
    {
        var result = new SwrResult<string>();
        string? capturedPropertyName = null;
        result.PropertyChanged += (_, e) => capturedPropertyName = e.PropertyName;

        result.IsLoading = true;

        capturedPropertyName.Should().Be("IsLoading");
    }

    [Fact]
    public void Setting_IsFromCache_Raises_PropertyChanged()
    {
        var result = new SwrResult<string>();
        string? capturedPropertyName = null;
        result.PropertyChanged += (_, e) => capturedPropertyName = e.PropertyName;

        result.IsFromCache = true;

        capturedPropertyName.Should().Be("IsFromCache");
    }

    [Fact]
    public void Setting_Error_Raises_PropertyChanged()
    {
        var result = new SwrResult<string>();
        string? capturedPropertyName = null;
        result.PropertyChanged += (_, e) => capturedPropertyName = e.PropertyName;

        result.Error = "fail";

        capturedPropertyName.Should().Be("Error");
    }

    [Fact]
    public void NotifyRevalidated_Fires_OnRevalidated_Event()
    {
        var result = new SwrResult<string>();
        var fired = false;
        result.OnRevalidated += () => fired = true;

        result.NotifyRevalidated();

        fired.Should().BeTrue();
    }
}
