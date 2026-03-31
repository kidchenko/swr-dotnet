namespace Swr.Net;

/// <summary>
/// Provides stale-while-revalidate caching for HTTP data fetching.
/// Register via <see cref="Extensions.SwrServiceCollectionExtensions.AddSwrForBlazor"/> or
/// <see cref="Extensions.SwrServiceCollectionExtensions.AddSwrForAspNetCore"/>.
/// </summary>
public interface ISwr : IDisposable
{
    /// <summary>
    /// Fetches data for the given cache key. Returns immediately with cached data if available,
    /// then revalidates in the background.
    /// </summary>
    /// <typeparam name="T">The type of data to fetch and cache.</typeparam>
    /// <param name="key">The URL or cache key identifying this data.</param>
    /// <param name="options">Per-request override of global SWR options, or null to use defaults.</param>
    /// <param name="onRevalidated">Optional callback invoked when background revalidation completes.</param>
    /// <returns>A <see cref="SwrResult{T}"/> that updates asynchronously as data flows through cache hit, fetch, and revalidation.</returns>
    SwrResult<T> Get<T>(string key, SwrOptions? options = null,
        Action<SwrResult<T>>? onRevalidated = null);

    /// <summary>
    /// Fetches data and awaits until the initial data is available. Use in Blazor's
    /// <c>OnInitializedAsync</c> to ensure the component renders with data on first paint.
    /// </summary>
    /// <typeparam name="T">The type of data to fetch and cache.</typeparam>
    /// <param name="key">The URL or cache key identifying this data.</param>
    /// <param name="options">Per-request override of global SWR options, or null to use defaults.</param>
    /// <returns>A task that completes with a <see cref="SwrResult{T}"/> once initial data is available.</returns>
    Task<SwrResult<T>> GetAsync<T>(string key, SwrOptions? options = null);

    /// <summary>
    /// Executes a mutation action and invalidates the specified cache keys.
    /// If the mutation throws, no cache keys are evicted.
    /// </summary>
    /// <param name="mutation">The async action to execute (e.g., a POST/PUT/DELETE call).</param>
    /// <param name="invalidateKeys">Cache keys to evict after the mutation completes successfully.</param>
    Task MutateAsync(Func<Task> mutation, params string[] invalidateKeys);

    /// <summary>
    /// Sends a POST request, deserializes the response, and invalidates the URL's cache entry
    /// plus any additional keys.
    /// </summary>
    /// <typeparam name="T">The type of the response body to deserialize.</typeparam>
    /// <param name="url">The URL to POST to. Also used as the primary cache key to invalidate.</param>
    /// <param name="content">The request body object, serialized as JSON.</param>
    /// <param name="additionalInvalidateKeys">Additional cache keys to evict after the request succeeds.</param>
    /// <returns>The deserialized response body, or null if the response body is empty.</returns>
    Task<T?> PostAsync<T>(string url, object content, params string[] additionalInvalidateKeys);

    /// <summary>
    /// Sends a PUT request, deserializes the response, and invalidates the URL's cache entry
    /// plus any additional keys.
    /// </summary>
    /// <typeparam name="T">The type of the response body to deserialize.</typeparam>
    /// <param name="url">The URL to PUT to. Also used as the primary cache key to invalidate.</param>
    /// <param name="content">The request body object, serialized as JSON.</param>
    /// <param name="additionalInvalidateKeys">Additional cache keys to evict after the request succeeds.</param>
    /// <returns>The deserialized response body, or null if the response body is empty.</returns>
    Task<T?> PutAsync<T>(string url, object content, params string[] additionalInvalidateKeys);

    /// <summary>
    /// Sends a DELETE request and invalidates the URL's cache entry plus any additional keys.
    /// </summary>
    /// <param name="url">The URL to DELETE. Also used as the primary cache key to invalidate.</param>
    /// <param name="additionalInvalidateKeys">Additional cache keys to evict after the request succeeds.</param>
    Task DeleteAsync(string url, params string[] additionalInvalidateKeys);

    /// <summary>
    /// Removes a specific cache entry by exact key.
    /// </summary>
    /// <param name="key">The exact cache key to remove.</param>
    void Invalidate(string key);

    /// <summary>
    /// Removes all cache entries whose keys start with the given prefix.
    /// Useful for invalidating related endpoints (e.g., all "/api/users" entries).
    /// </summary>
    /// <param name="prefix">The key prefix to match. All entries with keys starting with this prefix are removed.</param>
    void InvalidateByPrefix(string prefix);

    /// <summary>
    /// Removes all cache entries.
    /// </summary>
    void InvalidateAll();
}
