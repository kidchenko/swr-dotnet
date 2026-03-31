using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Swr.Net;

public sealed class SwrResult<T> : INotifyPropertyChanged, IDisposable
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private T? _data;
    private bool _isLoading;
    private bool _isFromCache;
    private string? _error;

    public T? Data
    {
        get => _data;
        internal set
        {
            _data = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        internal set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsFromCache
    {
        get => _isFromCache;
        internal set
        {
            _isFromCache = value;
            OnPropertyChanged();
        }
    }

    public string? Error
    {
        get => _error;
        internal set
        {
            _error = value;
            OnPropertyChanged();
        }
    }

    public Task Completed => _tcs.Task;

    public event Action? OnRevalidated;
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetCompleted() => _tcs.TrySetResult();

    internal void NotifyRevalidated()
    {
        _tcs.TrySetResult();
        OnRevalidated?.Invoke();
    }

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
