namespace Swr.Net;

public interface ISwr : IDisposable
{
    SwrResult<T> Get<T>(string key, SwrOptions? options = null,
        Action<SwrResult<T>>? onRevalidated = null);

    Task<SwrResult<T>> GetAsync<T>(string key, SwrOptions? options = null);

    Task MutateAsync(Func<Task> mutation, params string[] invalidateKeys);
    void Invalidate(string key);
    void InvalidateByPrefix(string prefix);
    void InvalidateAll();
}
