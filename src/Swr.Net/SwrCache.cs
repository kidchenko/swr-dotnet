using System.Collections.Concurrent;
using System.Net.Http.Json;
using Swr.Net.Keys;
using Swr.Net.Store;

namespace Swr.Net;

internal sealed class SwrCache : ISwr
{
    private readonly HttpClient _http;
    private readonly ISwrStore _store;
    private readonly SwrOptions _defaultOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _inflight = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public SwrCache(HttpClient http, ISwrStore store, SwrOptions? defaultOptions = null, TimeProvider? timeProvider = null)
    {
        _http = http;
        _store = store;
        _defaultOptions = defaultOptions ?? new SwrOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SwrResult<T> Get<T>(string key, SwrOptions? options = null, Action<SwrResult<T>>? onRevalidated = null)
    {
        var opts = options ?? _defaultOptions;
        var normalizedKey = SwrCacheKey.Normalize(key);
        var result = new SwrResult<T>();

        if (onRevalidated is not null)
            result.OnRevalidated += () => onRevalidated(result);

        if (_store.TryGet(normalizedKey, out var entry) && entry is not null)
        {
            var age = _timeProvider.GetUtcNow() - entry.StoredAt;

            if (age > opts.CacheTime)
            {
                // EXPIRED — evict and treat as MISS
                _store.Evict(normalizedKey);
                // fall through to MISS path
            }
            else if (age > opts.StaleTime)
            {
                // STALE — return cached data, revalidate in background
                result.Data = (T?)entry.Data;
                result.IsFromCache = true;
                result.IsLoading = true;
                result.SetCompleted(); // CRITICAL: Pitfall #4 — set completed before return
                FireAndForget(ct => RevalidateAsync<T>(key, normalizedKey, result, opts, ct), _cts.Token);
                return result;
            }
            else
            {
                // FRESH — return cached data, no background work
                result.Data = (T?)entry.Data;
                result.IsFromCache = true;
                result.IsLoading = false;
                result.SetCompleted(); // CRITICAL: Pitfall #4 — set completed for FRESH too
                return result;
            }
        }

        // MISS — fetch from network
        result.IsLoading = true;
        FireAndForget(ct => FetchAsync<T>(key, normalizedKey, result, opts, ct), _cts.Token);
        return result;
    }

    public async Task<SwrResult<T>> GetAsync<T>(string key, SwrOptions? options = null)
    {
        var result = Get<T>(key, options, onRevalidated: null);
        await result.Completed.ConfigureAwait(false);
        return result;
    }

    public Task MutateAsync(Func<Task> mutation, params string[] invalidateKeys)
        => throw new NotImplementedException("Implemented in Plan 03");

    public void Invalidate(string key)
        => _store.Evict(SwrCacheKey.Normalize(key));

    public void InvalidateByPrefix(string prefix)
        => _store.InvalidateByPrefix(prefix);

    public void InvalidateAll()
        => _store.Clear();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task FetchAsync<T>(string url, string normalizedKey, SwrResult<T> result, SwrOptions opts, CancellationToken ct)
    {
        try
        {
            var data = await FetchWithDeduplicationAsync<T>(url, normalizedKey, opts, ct);
            _store.Set(normalizedKey, data);
            result.Data = (T?)data;
            result.IsFromCache = false;
            result.IsLoading = false;
            result.NotifyRevalidated();
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.IsLoading = false;
            result.NotifyRevalidated();
        }
    }

    private async Task RevalidateAsync<T>(string url, string normalizedKey, SwrResult<T> result, SwrOptions opts, CancellationToken ct)
    {
        try
        {
            var data = await FetchWithDeduplicationAsync<T>(url, normalizedKey, opts, ct);
            _store.Set(normalizedKey, data);
            result.Data = (T?)data;
            result.IsFromCache = false;
            result.IsLoading = false;
            result.NotifyRevalidated();
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception)
        {
            // REQ-03: Background revalidation errors silent — preserve stale data
            result.IsLoading = false;
            result.NotifyRevalidated();
        }
    }

    private async Task<object?> FetchWithDeduplicationAsync<T>(string url, string normalizedKey, SwrOptions opts, CancellationToken ct)
    {
        if (opts.DeduplicateRequests)
        {
            var newTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_inflight.TryAdd(normalizedKey, newTcs))
            {
                // Lost the race — await the winner's result
                if (_inflight.TryGetValue(normalizedKey, out var existing))
                    return await existing.Task.WaitAsync(ct).ConfigureAwait(false);
                // Edge case: winner already removed — fall through to fetch
            }
            else
            {
                try
                {
                    var data = await FetchWithRetryAsync<T>(url, opts.RetryCount, opts.RetryBaseDelay, ct);
                    newTcs.TrySetResult(data);
                    return data;
                }
                catch (Exception ex)
                {
                    newTcs.TrySetException(ex);
                    throw;
                }
                finally
                {
                    _inflight.TryRemove(normalizedKey, out _);
                }
            }
        }

        return await FetchWithRetryAsync<T>(url, opts.RetryCount, opts.RetryBaseDelay, ct);
    }

    private async Task<object?> FetchWithRetryAsync<T>(string url, int retryCount, TimeSpan baseDelay, CancellationToken ct)
    {
        for (int attempt = 0; attempt <= retryCount; attempt++)
        {
            try
            {
                return await _http.GetFromJsonAsync<T>(url, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch when (attempt < retryCount)
            {
                var delay = TimeSpan.FromMilliseconds(
                    Math.Pow(2, attempt) * baseDelay.TotalMilliseconds);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
        // Final attempt failed — rethrow
        return await _http.GetFromJsonAsync<T>(url, ct).ConfigureAwait(false);
    }

    private void FireAndForget(Func<CancellationToken, Task> work, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await work(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* expected on shutdown */ }
            catch (Exception)
            {
                // Silently handle — individual paths already set error on result
            }
        }, ct);
    }
}
