---
title: Getting Started
description: Install SWR .NET and build your first stale-while-revalidate component in Blazor.
---

SWR .NET brings the stale-while-revalidate caching strategy to .NET. Serve cached data instantly while revalidating in the background — the user always sees data fast, and it's always eventually fresh.

## Prerequisites

- .NET 8.0 or later
- A Blazor Server, Blazor WebAssembly, or ASP.NET Core project

## Installation

```bash title="Terminal"
dotnet add package Swr.Net --prerelease
```

## Register Services

```csharp title="Program.cs"
builder.Services.AddSwrForBlazor();
```

:::tip
Using ASP.NET Core without Blazor? See the [ASP.NET Core Integration guide](/swr-dotnet/guides/aspnetcore-integration/) for singleton cache setup.
:::

## Basic Usage

```razor title="Components/Pages/Home.razor"
@using Swr.Net
@inject ISwr Swr
@implements IDisposable

@if (result is not null)
{
    @if (result.IsLoading)
    {
        <p>Loading...</p>
    }
    @if (result.Data is not null)
    {
        <h2>@result.Data.Name</h2>
    }
}

@code {
    private SwrResult<User>? result;

    protected override async Task OnInitializedAsync()
    {
        result = await Swr.GetAsync<User>("/api/users/me");
    }

    public void Dispose() => result?.Dispose();
}
```

- `GetAsync<T>` returns a `SwrResult<T>` that updates as data flows through cache → fetch → revalidation
- The URL is both the fetch endpoint and the cache key
- `@implements IDisposable` is required to prevent memory leaks from event subscriptions

## How It Works

1. **Stale** — Return cached data instantly. The UI is never blocked.
2. **Revalidate** — Fetch fresh data in the background.
3. **Update** — Swap in new data. Every subscriber re-renders automatically.

## Configuration

```csharp title="Program.cs"
builder.Services.AddSwrForBlazor(options =>
{
    options.StaleTime = TimeSpan.FromSeconds(30);
    options.CacheTime = TimeSpan.FromMinutes(5);
});
```

## Next Steps

- [Blazor Integration Guide](/swr-dotnet/guides/blazor-integration/) — Full lifecycle: loading states, revalidation callbacks, mutations, disposal
- [ASP.NET Core Integration Guide](/swr-dotnet/guides/aspnetcore-integration/) — Singleton cache, IHttpClientFactory, controllers and minimal APIs
- [API Reference](/swr-dotnet/reference/) — Complete documentation for all public types
