using System.Globalization;
using Backend.Models;

namespace Backend.Helpers;

public static class AynaLiveRouteUrlBuilder
{
    public static Uri BuildBusListUri(AynaLiveRouteOptions options)
    {
        return BuildUri(options.ApiBaseUrl, options.BusListPath);
    }

    public static Uri BuildBusByIdUri(AynaLiveRouteOptions options, int routeId)
    {
        return BuildUri(
            options.ApiBaseUrl,
            options.BusByIdPath,
            $"id={routeId.ToString(CultureInfo.InvariantCulture)}");
    }

    public static Uri BuildSiteUri(AynaLiveRouteOptions options)
    {
        return new Uri(options.SiteUrl, UriKind.Absolute);
    }

    private static Uri BuildUri(string baseUrl, string relativePath, string? query = null)
    {
        var builder = new UriBuilder(new Uri(new Uri(EnsureTrailingSlash(baseUrl)), relativePath.TrimStart('/')));

        if (!string.IsNullOrWhiteSpace(query))
        {
            builder.Query = query;
        }

        return builder.Uri;
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith('/') ? value : $"{value}/";
    }
}
