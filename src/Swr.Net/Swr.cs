using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Swr.Net.Keys;
using Swr.Net.Logging;
using Swr.Net.Store;

namespace Swr.Net;

internal sealed class Swr : ISwr
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISwrStore _store;
    private readonly SwrOptions _defaultOptions;
    private readonly ILogger<Swr> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _inflight = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public Swr(IHttpClientFactory httpClientFactory, ISwrStore store, IOptions<SwrOptions> options, ILogger<Swr> logger, TimeProvider? timeProvider = null)
    {
        _httpClientFactory = httpClientFactory;
        _store = store;
        _defaultOptions = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // Test constructor — allows direct HttpClient injection without IHttpClientFactory
    internal Swr(HttpClient http, ISwrStore store, SwrOptions? defaultOptions = null, TimeProvider? timeProvider = null)
    {
        _httpClientFactory = new SingleClientFactory(http);
        _store = store;
        _defaultOptions = defaultOptions ?? new SwrOptions();
        _logger = NullLogger<Swr>.Instance;
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
                _logger.LogCacheMiss(normalizedKey);
                // fall through to MISS path
            }
            else if (age > opts.StaleTime)
            {
                // STALE — return cached data, revalidate in background
                result.Data = (T?)entry.Data;
                result.IsFromCache = true;
                result.IsLoading = true;
                result.SetCompleted(); // CRITICAL: Pitfall #4 — set completed before return
                _logger.LogCacheStale(normalizedKey, age);
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
                _logger.LogCacheFresh(normalizedKey);
                return result;
            }
        }

        // MISS — fetch from network
        result.IsLoading = true;
        _logger.LogCacheMiss(normalizedKey);
        FireAndForget(ct => FetchAsync<T>(key, normalizedKey, result, opts, ct), _cts.Token);
        return result;
    }

    public async Task<SwrResult<T>> GetAsync<T>(string key, SwrOptions? options = null)
    {
        var result = Get<T>(key, options, onRevalidated: null);
        await result.Completed.ConfigureAwait(false);
        return result;
    }

    public async Task MutateAsync(Func<Task> mutation, params string[] invalidateKeys)
    {
        await mutation().ConfigureAwait(false);
        InvalidateKeys(invalidateKeys);
    }

    public async Task<T?> PostAsync<T>(string url, object content, params string[] additionalInvalidateKeys)
    {
        var http = _httpClientFactory.CreateClient("swr");
        var response = await http.PostAsJsonAsync(url, content, _cts.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>(_cts.Token).ConfigureAwait(false);
        InvalidateUrlAndKeys(url, additionalInvalidateKeys);
        return result;
    }

    public async Task<T?> PutAsync<T>(string url, object content, params string[] additionalInvalidateKeys)
    {
        var http = _httpClientFactory.CreateClient("swr");
        var response = await http.PutAsJsonAsync(url, content, _cts.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<T>(_cts.Token).ConfigureAwait(false);
        InvalidateUrlAndKeys(url, additionalInvalidateKeys);
        return result;
    }

    public async Task DeleteAsync(string url, params string[] additionalInvalidateKeys)
    {
        var http = _httpClientFactory.CreateClient("swr");
        var response = await http.DeleteAsync(url, _cts.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        InvalidateUrlAndKeys(url, additionalInvalidateKeys);
    }

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

    private void InvalidateKeys(string[] keys)
    {
        foreach (var key in keys)
            _store.Evict(SwrCacheKey.Normalize(key));
    }

    private void InvalidateUrlAndKeys(string url, string[] additionalKeys)
    {
        _store.Evict(SwrCacheKey.Normalize(url));
        foreach (var key in additionalKeys)
            _store.Evict(SwrCacheKey.Normalize(key));
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
            _logger.LogRevalidationComplete(normalizedKey);
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            // REQ-03: Background revalidation errors silent — preserve stale data
            _logger.LogRevalidationFailed(normalizedKey, ex.Message);
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
        var http = _httpClientFactory.CreateClient("swr");
        for (int attempt = 0; attempt <= retryCount; attempt++)
        {
            try
            {
                return await http.GetFromJsonAsync<T>(url, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (attempt < retryCount)
            {
                _logger.LogRetry(attempt + 1, retryCount + 1, url, ex.Message);
                var delay = TimeSpan.FromMilliseconds(
                    Math.Pow(2, attempt) * baseDelay.TotalMilliseconds);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
        // Final attempt — may throw
        try
        {
            return await http.GetFromJsonAsync<T>(url, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogFetchFailed(url, retryCount + 1, ex.Message);
            throw;
        }
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

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public SingleClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }
}
