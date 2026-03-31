using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swr.Net;
using Swr.Net.Extensions;
using Swr.Net.Store;
using Xunit;

namespace Swr.Net.Tests.ExtensionTests;

public class BlazorRegistrationTests
{
    [Fact]
    public void AddSwrForBlazor_RegistersISwrAsScoped()
    {
        var services = new ServiceCollection();
        services.AddSwrForBlazor();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ISwr) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSwrForBlazor_RegistersISwrStoreAsScoped()
    {
        var services = new ServiceCollection();
        services.AddSwrForBlazor();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ISwrStore) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSwrForBlazor_ResolvesISwr()
    {
        var services = new ServiceCollection();
        services.AddSwrForBlazor();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var swr = scope.ServiceProvider.GetRequiredService<ISwr>();

        swr.Should().NotBeNull();
        swr.Should().BeAssignableTo<ISwr>();
    }

    [Fact]
    public void AddSwrForBlazor_ScopedIsolation_DifferentScopesGetDifferentInstances()
    {
        var services = new ServiceCollection();
        services.AddSwrForBlazor();

        using var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var swr1 = scope1.ServiceProvider.GetRequiredService<ISwr>();
        var swr2 = scope2.ServiceProvider.GetRequiredService<ISwr>();

        swr1.Should().NotBeSameAs(swr2);
    }

    [Fact]
    public void AddSwrForBlazor_SameScopeReturnsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddSwrForBlazor();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var swr1 = scope.ServiceProvider.GetRequiredService<ISwr>();
        var swr2 = scope.ServiceProvider.GetRequiredService<ISwr>();

        swr1.Should().BeSameAs(swr2);
    }

    [Fact]
    public void AddSwrForBlazor_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddSwrForBlazor(opts =>
        {
            opts.StaleTime = TimeSpan.FromMinutes(5);
            opts.CacheTime = TimeSpan.FromMinutes(30);
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SwrOptions>>();

        options.Value.StaleTime.Should().Be(TimeSpan.FromMinutes(5));
        options.Value.CacheTime.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void AddSwrForBlazor_WithoutConfigure_UsesDefaults()
    {
        var services = new ServiceCollection();
        services.AddSwrForBlazor();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SwrOptions>>();

        options.Value.StaleTime.Should().Be(TimeSpan.FromMinutes(2));
        options.Value.CacheTime.Should().Be(TimeSpan.FromMinutes(10));
    }
}
