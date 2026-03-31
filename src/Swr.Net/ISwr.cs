namespace Swr.Net;

public interface ISwr : IDisposable
{
    SwrResult<T> Get<T>(string key, SwrOptions? options = null,
        Action<SwrResult<T>>? onRevalidated = null);

    Task<SwrResult<T>> GetAsync<T>(string key, SwrOptions? options = null);

    Task MutateAsync(Func<Task> mutation, params string[] invalidateKeys);

    Task<T?> PostAsync<T>(string url, object content, params string[] additionalInvalidateKeys);
    Task<T?> PutAsync<T>(string url, object content, params string[] additionalInvalidateKeys);
    Task DeleteAsync(string url, params string[] additionalInvalidateKeys);

    void Invalidate(string key);
    void InvalidateByPrefix(string prefix);
    void InvalidateAll();
}
