using System.Net;
using System.Text.Json;

namespace Swr.Net.Tests.Helpers;

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
    private int _callCount;

    public int CallCount => _callCount;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => _handler = handler;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Interlocked.Increment(ref _callCount);
        return await _handler(request, ct);
    }
}

internal static class HttpResponseHelper
{
    public static HttpResponseMessage JsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}

internal static class SwrCacheFactory
{
    public static (Swr cache, MockHttpMessageHandler handler, global::Swr.Net.Store.InMemorySwrCacheStore store, Microsoft.Extensions.Time.Testing.FakeTimeProvider time) Create(
        SwrOptions? options = null,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handlerFunc = null)
    {
        var time = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.UtcNow);
        var store = new global::Swr.Net.Store.InMemorySwrCacheStore(time);
        var handler = new MockHttpMessageHandler(handlerFunc ?? ((req, ct) =>
            Task.FromResult(HttpResponseHelper.JsonResponse("default"))));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://test.local") };
        var cache = new Swr(http, store, options, time);
        return (cache, handler, store, time);
    }
}
