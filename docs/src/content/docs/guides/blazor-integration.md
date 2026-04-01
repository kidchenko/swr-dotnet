---
title: Blazor Integration
description: Complete guide to using Swr.Net in Blazor Server and Blazor WebAssembly applications.
---

Swr.Net works with both Blazor Server and Blazor WebAssembly. `AddSwrForBlazor()` registers `ISwr` as **Scoped**, meaning each Blazor circuit gets its own independent cache — user data never leaks between sessions.

## Setup

```csharp title="Program.cs"
builder.Services.AddSwrForBlazor();
```

:::caution[Lifetime matters]
`AddSwrForBlazor()` registers `ISwr` as **Scoped** (per Blazor circuit). Do not use `AddSwrForAspNetCore()` in Blazor — this registers a Singleton, causing cache data to leak between users.
:::

Optionally configure global defaults:

```csharp title="Program.cs"
builder.Services.AddSwrForBlazor(options =>
{
    options.StaleTime = TimeSpan.FromSeconds(30);
    options.CacheTime = TimeSpan.FromMinutes(5);
});
```

## Fetching Data

Use `GetAsync<T>` in `OnInitializedAsync` to load data when the component initializes:

```razor title="Components/Pages/Weather.razor"
@using Swr.Net
@inject ISwr Swr
@implements IDisposable

@if (result?.IsLoading == true)
{
    <p>Loading...</p>
}
else if (result?.Error is not null)
{
    <p class="error">@result.Error</p>
}
else if (result?.Data is not null)
{
    <h2>@result.Data.Summary</h2>
    <p>Temperature: @result.Data.TemperatureC °C</p>
}

@code {
    private SwrResult<WeatherForecast>? result;

    protected override async Task OnInitializedAsync()
    {
        result = await Swr.GetAsync<WeatherForecast>("/api/weather");
    }

    public void Dispose() => result?.Dispose();
}
```

- `GetAsync<T>` awaits until initial data is available — either from cache (instant) or from the network
- The URL is both the fetch endpoint and the cache key
- `@using Swr.Net` imports the namespace for `ISwr`, `SwrResult<T>`, and related types

## Loading and Error States

A `SwrResult<T>` can be in three states:

1. `IsLoading == true` — a network request is in progress
2. `Error is not null` — the fetch failed; check `Error` for the message
3. `Data is not null` — data is available (may be fresh or from cache)

:::tip
When stale data is served from the cache, `IsFromCache` is `true` and a background revalidation is already running. Use this to show a subtle "Refreshing..." indicator so users know data may update shortly.
:::

```razor
@if (result?.Data is not null)
{
    <h2>@result.Data.Name</h2>
    @if (result.IsFromCache)
    {
        <span class="badge">Refreshing...</span>
    }
}
```

## Background Revalidation

Subscribe to `OnRevalidated` to re-render when fresh data arrives after a background fetch:

```razor title="Components/Pages/UserProfile.razor" {14}
@using Swr.Net
@inject ISwr Swr
@implements IDisposable

@if (result?.Data is not null)
{
    <h2>@result.Data.Name</h2>
    <p>@result.Data.Email</p>
    @if (result.IsFromCache)
    {
        <span>Refreshing...</span>
    }
}

@code {
    private SwrResult<UserProfile>? result;

    protected override async Task OnInitializedAsync()
    {
        result = await Swr.GetAsync<UserProfile>("/api/users/me");
        result.OnRevalidated += () => InvokeAsync(StateHasChanged);
    }

    public void Dispose() => result?.Dispose();
}
```

:::caution[Threading]
`OnRevalidated` fires from a background thread. In Blazor Server, you **must** use `InvokeAsync(StateHasChanged)` to marshal the call to the render context. Calling `StateHasChanged()` directly will throw or silently fail.
:::

## Per-Request Options

Override global options for a specific fetch:

```csharp
var options = new SwrOptions
{
    StaleTime = TimeSpan.FromSeconds(10),
    CacheTime = TimeSpan.FromMinutes(1),
    RetryCount = 5
};
result = await Swr.GetAsync<Dashboard>("/api/dashboard", options);
```

Per-request options take precedence over the global options configured in `Program.cs`.

## Composite Cache Keys

When the same endpoint returns different data depending on context (e.g., per-user or per-tenant), use `SwrCacheKey.From` to construct a unique key:

```csharp
@using Swr.Net.Keys

// Creates a key like "/api/profile::user-123"
var key = SwrCacheKey.From("/api/profile", userId);
result = await Swr.GetAsync<Profile>(key);
```

The `::` separator ensures the constructed key cannot collide with a real URL. Different users fetching the same endpoint get isolated cache entries.

## Mutations

### MutateAsync

Use `MutateAsync` to execute a mutation and invalidate related cache entries:

```csharp
// Execute a mutation and invalidate related cache entries
await Swr.MutateAsync(
    async () => await Http.PutAsJsonAsync($"/api/users/{userId}", updates),
    "/api/users/me", "/api/users"
);
```

If the mutation throws, no cache keys are evicted — the cache stays consistent.

### Convenience Methods

Use `PostAsync<T>`, `PutAsync<T>`, and `DeleteAsync` for common HTTP mutations with automatic invalidation:

```csharp
// POST — creates a resource and invalidates the list cache
var created = await Swr.PostAsync<Todo>("/api/todos", new { Title = "Buy milk" }, "/api/todos");

// PUT — updates a resource and invalidates its cache entry
var updated = await Swr.PutAsync<Todo>($"/api/todos/{id}", updates);

// DELETE — deletes a resource and invalidates the list cache
await Swr.DeleteAsync($"/api/todos/{id}", "/api/todos");
```

The URL is always invalidated automatically; pass additional keys to invalidate related entries.

### Manual Invalidation

Force a cache refresh without executing a mutation:

```csharp
Swr.Invalidate("/api/users/me");           // Remove one exact key
Swr.InvalidateByPrefix("/api/users");       // Remove all keys starting with prefix
Swr.InvalidateAll();                        // Clear the entire cache
```

## Component Disposal

`SwrResult<T>` holds event subscriptions (`OnRevalidated`, `PropertyChanged`). Without disposal, these subscriptions are never released, causing a memory leak on each component mount.

Every Blazor component that uses `ISwr` must implement `IDisposable`:

```razor
@implements IDisposable

@code {
    private SwrResult<User>? result;

    protected override async Task OnInitializedAsync()
    {
        result = await Swr.GetAsync<User>("/api/users/me");
        result.OnRevalidated += () => InvokeAsync(StateHasChanged);
    }

    public void Dispose() => result?.Dispose();
}
```

The `?.` null-conditional operator is safe here — if `OnInitializedAsync` throws before `result` is assigned, `Dispose` does nothing.

## Next Steps

- [Getting Started](/swr-dotnet/getting-started/introduction/) — Installation and first component
- [ASP.NET Core Integration](/swr-dotnet/guides/aspnetcore-integration/) — Singleton cache for server-side usage
- [API Reference](/swr-dotnet/reference/) — Complete documentation for all public types
