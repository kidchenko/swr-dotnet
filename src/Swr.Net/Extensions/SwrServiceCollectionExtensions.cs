using Microsoft.Extensions.DependencyInjection;
using Swr.Net.Store;

namespace Swr.Net.Extensions;

/// <summary>
/// Extension methods for registering Swr.Net services with the DI container.
/// </summary>
public static class SwrServiceCollectionExtensions
{
    /// <summary>
    /// Registers Swr.Net services for Blazor applications.
    /// ISwr and ISwrStore are registered as Scoped — each Blazor circuit gets its own isolated cache.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration callback for SwrOptions (StaleTime, CacheTime, etc.).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwrForBlazor(
        this IServiceCollection services,
        Action<SwrOptions>? configure = null)
    {
        services.AddHttpClient("swr");

        if (configure is not null)
            services.Configure<SwrOptions>(configure);
        else
            services.AddOptions<SwrOptions>();

        services.AddScoped<ISwrStore, InMemorySwrCacheStore>();
        services.AddScoped<ISwr, Swr>();

        return services;
    }

    /// <summary>
    /// Registers Swr.Net services for ASP.NET Core applications.
    /// ISwr and ISwrStore are registered as Singleton — the cache is shared across all requests.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration callback for SwrOptions (StaleTime, CacheTime, etc.).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwrForAspNetCore(
        this IServiceCollection services,
        Action<SwrOptions>? configure = null)
    {
        services.AddHttpClient("swr");

        if (configure is not null)
            services.Configure<SwrOptions>(configure);
        else
            services.AddOptions<SwrOptions>();

        services.AddSingleton<ISwrStore, InMemorySwrCacheStore>();
        services.AddSingleton<ISwr, Swr>();

        return services;
    }
}
