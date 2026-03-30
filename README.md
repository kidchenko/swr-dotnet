# SWR .NET

A data fetching library for Blazor that implements the **stale-while-revalidate** caching strategy.

Serve cached data first for instant UI, fetch fresh data in the background, and update all subscribers seamlessly.

## How it works

1. **Stale** — Return cached data instantly. The UI is never blocked.
2. **Revalidate** — Fetch fresh data in the background.
3. **Update** — Swap in new data. Every subscriber re-renders automatically.

## Installation

```bash
dotnet add package Swr.Net
```

## Quick start

Register the services in `Program.cs`:

```csharp
builder.Services.AddSwr();
```

Use it in any Blazor component:

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

The first argument is a **cache key**. The second is a **fetcher** — an async function that retrieves the data. When another component uses the same key, it gets the cached value instantly.

## Features

- **Global Cache** — One fetch, every subscriber stays in sync
- **Auto Revalidation** — Refreshes on focus, reconnect, and intervals
- **Blazor Native** — Built for Server and WebAssembly
- **Type Safe** — Full generic support with compile-time checking

## Documentation

[https://kidchenko.github.io/swr-dotnet](https://kidchenko.github.io/swr-dotnet)

## License

[MIT](LICENSE)
