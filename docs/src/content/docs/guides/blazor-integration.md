---
title: Blazor Integration
description: Full lifecycle guide for using SWR .NET in Blazor Server and Blazor WebAssembly applications.
---

This guide covers the full lifecycle of SWR .NET in Blazor applications: loading states, revalidation callbacks, mutations, and proper component disposal.

## Registration

Register SWR .NET services as scoped (per Blazor circuit) in `Program.cs`:

```csharp title="Program.cs"
builder.Services.AddSwrForBlazor();
```

With custom options:

```csharp title="Program.cs"
builder.Services.AddSwrForBlazor(options =>
{
    options.StaleTime = TimeSpan.FromSeconds(30);
    options.CacheTime = TimeSpan.FromMinutes(5);
    options.RetryCount = 3;
});
```

## Loading States

`SwrResult<T>` tracks loading state via the `IsLoading` property:

```razor title="Components/Pages/UserProfile.razor"
@using Swr.Net
@inject ISwr Swr
@implements IDisposable

@if (result is null || result.IsLoading)
{
    <p>Loading...</p>
}
else if (result.Error is not null)
{
    <p>Error: @result.Error</p>
}
else if (result.Data is not null)
{
    <h2>@result.Data.Name</h2>
    <p>@result.Data.Email</p>
    @if (result.IsFromCache)
    {
        <small>Showing cached data</small>
    }
}

@code {
    private SwrResult<UserDto>? result;

    protected override async Task OnInitializedAsync()
    {
        result = await Swr.GetAsync<UserDto>("/api/users/me");
    }

    public void Dispose() => result?.Dispose();
}
```

## Revalidation Callbacks

Use `OnRevalidated` to trigger UI updates after background revalidation completes:

```razor title="Components/Pages/Dashboard.razor"
@using Swr.Net
@inject ISwr Swr
@implements IDisposable

@code {
    private SwrResult<DashboardDto>? result;

    protected override async Task OnInitializedAsync()
    {
        result = Swr.Get<DashboardDto>("/api/dashboard",
            onRevalidated: r => InvokeAsync(StateHasChanged));
        await result.Completed;
    }

    public void Dispose() => result?.Dispose();
}
```

## Mutations

Use `MutateAsync` to execute a mutation and invalidate related cache keys:

```csharp title="Components/Pages/EditUser.razor.cs"
await Swr.MutateAsync(
    mutation: () => Http.PutAsJsonAsync("/api/users/me", model),
    invalidateKeys: ["/api/users/me", "/api/dashboard"]
);
```

## Disposal

Always implement `IDisposable` on components that use SWR. The `Dispose()` call on `SwrResult<T>` unsubscribes event listeners and prevents memory leaks:

```csharp
public void Dispose()
{
    result1?.Dispose();
    result2?.Dispose();
}
```
