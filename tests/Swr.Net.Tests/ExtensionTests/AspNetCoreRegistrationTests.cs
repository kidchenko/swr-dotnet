using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swr.Net;
using Swr.Net.Extensions;
using Swr.Net.Store;
using Xunit;

namespace Swr.Net.Tests.ExtensionTests;

public class AspNetCoreRegistrationTests
{
    [Fact]
    public void AddSwrForAspNetCore_RegistersISwrAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddSwrForAspNetCore();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ISwr) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSwrForAspNetCore_RegistersISwrStoreAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddSwrForAspNetCore();

        services.Should().Contain(d =>
            d.ServiceType == typeof(ISwrStore) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSwrForAspNetCore_ResolvesISwr()
    {
        var services = new ServiceCollection();
        services.AddSwrForAspNetCore();

        using var provider = services.BuildServiceProvider();
        var swr = provider.GetRequiredService<ISwr>();

        swr.Should().NotBeNull();
    }

    [Fact]
    public void AddSwrForAspNetCore_Singleton_SameInstanceAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddSwrForAspNetCore();

        using var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var swr1 = scope1.ServiceProvider.GetRequiredService<ISwr>();
        var swr2 = scope2.ServiceProvider.GetRequiredService<ISwr>();

        swr1.Should().BeSameAs(swr2);
    }

    [Fact]
    public void AddSwrForAspNetCore_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddSwrForAspNetCore(opts =>
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
    public void AddSwrForAspNetCore_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddSwrForAspNetCore();

        result.Should().BeSameAs(services);
    }
}
