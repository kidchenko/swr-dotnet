# Swr.Net

[![NuGet](https://img.shields.io/nuget/vpre/Swr.Net)](https://www.nuget.org/packages/Swr.Net)

A .NET library that brings the stale-while-revalidate (SWR) caching strategy to Blazor and ASP.NET Core. Serve cached data instantly while revalidating in the background — the user always sees data fast, and it's always eventually fresh.

## How it works

1. **Stale** — Return cached data instantly. The UI is never blocked.
2. **Revalidate** — Fetch fresh data in the background.
3. **Update** — Swap in new data. Every subscriber re-renders automatically.

## Installation

```bash
dotnet add package Swr.Net --prerelease
```

## Quick start — Blazor

Register the services in `Program.cs`:

```csharp
builder.Services.AddSwrForBlazor();
```

Use `ISwr` in any Blazor component:

```csharp
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

## Quick start — ASP.NET Core

Register the services in `Program.cs`:

```csharp
builder.Services.AddSwrForAspNetCore();
```

## Features

- **Stale-While-Revalidate** — Instant UI with cached data, fresh data in the background
- **Request Deduplication** — Concurrent requests for the same key share one network call
- **Automatic Retry** — Exponential backoff on failure with configurable attempts
- **Cache Mutations** — Invalidate by key, prefix, or clear all
- **Reactive Results** — `INotifyPropertyChanged` + `OnRevalidated` event for UI binding
- **Platform-Aware DI** — Scoped for Blazor (per-circuit), Singleton for ASP.NET Core
- **Pluggable Storage** — `ISwrStore` interface for custom cache backends

## Documentation

[https://kidchenko.github.io/swr-dotnet](https://kidchenko.github.io/swr-dotnet)

## License

[MIT](LICENSE)
