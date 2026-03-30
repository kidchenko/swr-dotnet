---
title: Getting Started
description: Install SWR .NET and build your first stale-while-revalidate component in Blazor.
---

SWR .NET is a data fetching library for Blazor that implements the **stale-while-revalidate** caching strategy. It serves cached data first for instant UI, fetches fresh data in the background, and updates all subscribers seamlessly.

## Prerequisites

- .NET 8.0 or later
- A Blazor Server or Blazor WebAssembly project

## Installation

Add the NuGet package to your project:

```bash
dotnet add package Swr.Net
```

## Register Services

Add SWR to your dependency injection container in `Program.cs`:

```csharp
builder.Services.AddSwr();
```

## Basic Usage

Inject `ISwr` into any Blazor component and start fetching:

```csharp
@inject ISwr Swr
@inject HttpClient Http

@if (user is not null)
{
    <h2>@user.Name</h2>
    <p>@user.Email</p>
}

@code {
    private User? user;

    protected override async Task OnInitializedAsync()
    {
        user = await Swr.GetAsync<User>(
            "user-profile",
            () => Http.GetFromJsonAsync<User>("/api/me")
        );
    }
}
```

The first argument is a **cache key** — any string that uniquely identifies this data. The second is a **fetcher** — an async function that retrieves the data.

When another component uses the same key, it gets the cached value instantly instead of making a duplicate request.

## How It Works

1. **Stale** — Return cached data immediately so the UI is never blocked.
2. **Revalidate** — Fetch fresh data in the background.
3. **Update** — Swap in the new data and re-render all subscribers.

This pattern ensures your users always see data instantly while keeping it fresh behind the scenes.

## Next Steps

- Configuration options (refresh intervals, retry, deduplication)
- Error handling and loading states
- Cache invalidation and mutation
- API reference
