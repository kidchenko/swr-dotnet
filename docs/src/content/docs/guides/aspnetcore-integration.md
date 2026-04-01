---
title: ASP.NET Core Integration
description: Use Swr.Net as a server-side cache in ASP.NET Core controllers and minimal APIs.
---

Swr.Net works as a server-side caching layer in ASP.NET Core. `AddSwrForAspNetCore()` registers `ISwr` as a **Singleton**, so the cache is shared across all HTTP requests. Use it to cache upstream API calls, database queries, or any expensive data source — requests benefit from cached results without hitting the upstream on every call.

## Setup

```csharp title="Program.cs"
builder.Services.AddSwrForAspNetCore();
```

:::note
`AddSwrForAspNetCore()` automatically registers a named `HttpClient` called `'swr'` via `IHttpClientFactory`. You do not need to register an `HttpClient` separately.
:::

Optionally configure global defaults:

```csharp title="Program.cs"
builder.Services.AddSwrForAspNetCore(options =>
{
    options.StaleTime = TimeSpan.FromSeconds(30);
    options.CacheTime = TimeSpan.FromMinutes(5);
});
```

:::tip
The cache is shared across all HTTP requests. When one request fetches data from an upstream API, subsequent requests within the `StaleTime` window get the cached result instantly — no upstream call is made. This is ideal for caching external service responses that are expensive or rate-limited.
:::

## Using ISwr in Controllers

Inject `ISwr` via primary constructor (C# 12+):

```csharp title="Controllers/ProductsController.cs"
using Swr.Net;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ISwr swr) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await swr.GetAsync<List<Product>>("/api/external/products");

        if (result.Error is not null)
            return StatusCode(503, new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await swr.GetAsync<Product>($"/api/external/products/{id}");

        if (result.Error is not null)
            return StatusCode(503, new { error = result.Error });

        if (result.Data is null)
            return NotFound();

        return Ok(result.Data);
    }
}
```

How it works:

- On cache **miss**: `GetAsync<T>` fetches from the upstream URL and caches the result
- On cache **hit** (within `StaleTime`): the response is instant — no upstream call
- On **stale hit** (past `StaleTime`, within `CacheTime`): stale data is returned immediately while a background revalidation runs
- Always check `result.Error` before accessing `result.Data` — a failed upstream call sets `Error` without throwing

## Using ISwr in Minimal APIs

```csharp title="Program.cs"
builder.Services.AddSwrForAspNetCore(options =>
{
    options.StaleTime = TimeSpan.FromSeconds(30);
    options.CacheTime = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

app.MapGet("/api/products", async (ISwr swr) =>
{
    var result = await swr.GetAsync<List<Product>>("/api/external/products");
    return result.Error is not null
        ? Results.Problem(result.Error)
        : Results.Ok(result.Data);
});

app.MapGet("/api/products/{id}", async (int id, ISwr swr) =>
{
    var result = await swr.GetAsync<Product>($"/api/external/products/{id}");
    return result.Error is not null
        ? Results.Problem(result.Error)
        : Results.Ok(result.Data);
});
```

## Cache Sharing

Because `ISwr` is a Singleton, the cache is shared across all requests:

1. **Request A** fetches `/api/external/products` → cache miss → fetches from upstream → result cached
2. **Request B** (within `StaleTime`) fetches the same key → cache hit → instant response, no upstream call
3. **Request C** (past `StaleTime`, within `CacheTime`) → stale data returned immediately → background revalidation starts
4. **Request D** (past `CacheTime`) → cache miss → full fetch from upstream

:::caution[Singleton scope]
Because the cache is shared, do **not** cache user-specific data by URL alone. If two users call `/api/external/profile`, they would receive each other's data. Use `SwrCacheKey.From` to create per-user keys instead.
:::

Use `SwrCacheKey.From` for per-user (or per-tenant) caching:

```csharp
using Swr.Net.Keys;

app.MapGet("/api/profile", async (ISwr swr, HttpContext context) =>
{
    var userId = context.User.FindFirst("sub")?.Value ?? "anonymous";
    var key = SwrCacheKey.From("/api/external/profile", userId);
    var result = await swr.GetAsync<UserProfile>(key);
    return result.Error is not null
        ? Results.Problem(result.Error)
        : Results.Ok(result.Data);
});
```

The `::` separator produces a key like `/api/external/profile::user-123`, which is unique per user while still being grouped under the same logical endpoint.

## IHttpClientFactory

`AddSwrForAspNetCore()` calls `services.AddHttpClient("swr")` internally, registering a named `HttpClient` that the library uses for all outbound requests. You can configure this named client like any other `IHttpClientFactory` registration:

```csharp title="Program.cs"
builder.Services.AddSwrForAspNetCore();

// Extend the 'swr' named HttpClient with your configuration
builder.Services.AddHttpClient("swr", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("X-API-Key", builder.Configuration["ApiKey"]);
});
```

:::note
Calling `AddHttpClient("swr")` after `AddSwrForAspNetCore()` does not create a duplicate registration — it extends the existing named client with your configuration. The library's internal registration and your configuration are merged by `IHttpClientFactory`.
:::

## Mutations and Cache Invalidation

### Convenience Methods in Controllers

```csharp title="Controllers/ProductsController.cs"
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
{
    var product = await swr.PostAsync<Product>(
        "/api/external/products",
        request,
        "/api/external/products"  // invalidate the list cache
    );

    return CreatedAtAction(nameof(GetById), new { id = product?.Id }, product);
}

[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    await swr.DeleteAsync(
        $"/api/external/products/{id}",
        "/api/external/products"  // invalidate the list cache
    );

    return NoContent();
}
```

### Manual Invalidation

Force a cache refresh without executing a mutation:

```csharp
swr.Invalidate("/api/external/products");          // Remove one exact key
swr.InvalidateByPrefix("/api/external/products");   // Remove all keys with this prefix
swr.InvalidateAll();                                 // Clear the entire cache
```

## Disposal

In ASP.NET Core with Singleton registration, `ISwr` lives for the lifetime of the application. You do **not** need to manually dispose it — the DI container handles disposal when the application shuts down.

This is different from Blazor, where each component disposes its own `SwrResult<T>` in `IDisposable.Dispose()`. In ASP.NET Core, `GetAsync<T>` responses are short-lived per-request values that do not require disposal.

## Next Steps

- [Getting Started](/swr-dotnet/getting-started/introduction/) — Installation and first example
- [Blazor Integration](/swr-dotnet/guides/blazor-integration/) — Scoped cache, OnRevalidated callbacks, component disposal
- [API Reference](/swr-dotnet/reference/) — Complete documentation for all public types
