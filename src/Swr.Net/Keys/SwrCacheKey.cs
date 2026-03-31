namespace Swr.Net.Keys;

public static class SwrCacheKey
{
    public static string Normalize(string url)
    {
        var queryIndex = url.IndexOf('?');
        if (queryIndex < 0) return url;

        var path = url[..queryIndex];
        var query = url[(queryIndex + 1)..];
        var parts = query.Split('&');
        Array.Sort(parts, StringComparer.Ordinal);
        return $"{path}?{string.Join('&', parts)}";
    }

    public static string From(string url, params string[] context)
    {
        var normalized = Normalize(url);
        if (context.Length == 0) return normalized;
        return $"{normalized}::{string.Join("::", context)}";
    }
}
