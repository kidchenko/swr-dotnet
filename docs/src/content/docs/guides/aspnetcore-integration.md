---
title: ASP.NET Core Integration
description: Guide for using SWR .NET in ASP.NET Core applications with singleton cache, IHttpClientFactory, controllers, and minimal APIs.
---

This guide covers using SWR .NET in ASP.NET Core applications: singleton cache setup, `IHttpClientFactory`, controllers, and minimal APIs.

## Registration

Register SWR .NET services as singleton (shared cache across all requests) in `Program.cs`:

```csharp title="Program.cs"
builder.Services.AddSwrForAspNetCore();
```

With custom options:

```csharp title="Program.cs"
builder.Services.AddSwrForAspNetCore(options =>
{
    options.StaleTime = TimeSpan.FromMinutes(1);
    options.CacheTime = TimeSpan.FromMinutes(30);
});
```

## Minimal APIs

Inject `ISwr` directly into minimal API handlers:

```csharp title="Program.cs"
app.MapGet("/api/products", async (ISwr swr) =>
{
    var result = await swr.GetAsync<List<Product>>("/products");
    return result.Data;
});
```

## Controllers

Inject `ISwr` via constructor:

```csharp title="Controllers/ProductsController.cs"
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISwr _swr;

    public ProductsController(ISwr swr)
    {
        _swr = swr;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var result = await _swr.GetAsync<List<Product>>("/api/upstream/products");
        return Ok(result.Data);
    }
}
```

## Cache Invalidation

Invalidate cache entries after mutations:

```csharp
await _swr.MutateAsync(
    mutation: () => _httpClient.PostAsJsonAsync("/api/upstream/products", newProduct),
    invalidateKeys: ["/api/upstream/products"]
);
```

Invalidate by prefix to clear related entries:

```csharp
_swr.InvalidateByPrefix("/api/upstream/products");
```

## Singleton Lifetime Note

`AddSwrForAspNetCore` registers `ISwr` as a **singleton**. The cache is shared across all requests and users. If you need per-user isolation, use `AddSwrForBlazor` (scoped) or implement a custom `ISwrStore` with user-scoped partitioning.
