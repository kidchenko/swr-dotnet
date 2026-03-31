using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Swr.Net;

/// <summary>
/// Represents the result of an SWR cache lookup. Properties update asynchronously as data flows
/// through cache hit, network fetch, and background revalidation.
/// </summary>
/// <typeparam name="T">The type of the fetched data.</typeparam>
public sealed class SwrResult<T> : INotifyPropertyChanged, IDisposable
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private T? _data;
    private bool _isLoading;
    private bool _isFromCache;
    private string? _error;

    /// <summary>
    /// The fetched data, or default if still loading or an error occurred.
    /// </summary>
    public T? Data
    {
        get => _data;
        internal set
        {
            _data = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// True while a network request is in progress (initial fetch or background revalidation).
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        internal set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// True if the current <see cref="Data"/> value was served from cache rather than a fresh network response.
    /// </summary>
    public bool IsFromCache
    {
        get => _isFromCache;
        internal set
        {
            _isFromCache = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// The error message if the fetch failed, or null on success.
    /// </summary>
    public string? Error
    {
        get => _error;
        internal set
        {
            _error = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// A task that completes when the initial data is available. Await this in Blazor's
    /// <c>OnInitializedAsync</c> to block rendering until data is ready.
    /// </summary>
    public Task Completed => _tcs.Task;

    /// <summary>
    /// Fires when background revalidation completes, whether successful or not.
    /// Use this to trigger UI updates after fresh data arrives.
    /// </summary>
    public event Action? OnRevalidated;

    /// <summary>
    /// Standard <see cref="INotifyPropertyChanged"/> event. Fires on <see cref="Data"/>,
    /// <see cref="IsLoading"/>, <see cref="IsFromCache"/>, and <see cref="Error"/> changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetCompleted() => _tcs.TrySetResult();

    internal void NotifyRevalidated()
    {
        _tcs.TrySetResult();
        OnRevalidated?.Invoke();
    }

    /// <summary>
    /// Cleans up event subscriptions to prevent memory leaks. Call this in Blazor component disposal.
    /// </summary>
    public void Dispose()
    {
        OnRevalidated = null;
        _tcs.TrySetCanceled();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
