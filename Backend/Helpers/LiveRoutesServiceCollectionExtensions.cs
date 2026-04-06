using System.Net.Http.Headers;
using Backend.Models;
using Backend.Services;
using Microsoft.Extensions.Options;

namespace Backend.Helpers;

public static class LiveRoutesServiceCollectionExtensions
{
    public static IServiceCollection AddLiveRoutes(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LiveRoutesOptions>()
            .Bind(configuration.GetSection(LiveRoutesOptions.SectionName));

        services.AddOptions<AynaLiveRouteOptions>()
            .Bind(configuration.GetSection(AynaLiveRouteOptions.SectionName));

        services.AddHttpClient(HttpBasedLiveRouteProvider.ClientName)
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var liveRoutesOptions = serviceProvider.GetRequiredService<IOptions<LiveRoutesOptions>>().Value;
                var aynaOptions = serviceProvider.GetRequiredService<IOptions<AynaLiveRouteOptions>>().Value;

                client.BaseAddress = new Uri(aynaOptions.ApiBaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(Math.Max(liveRoutesOptions.HttpTimeoutSeconds, 15));
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AYNA-Backend/1.0");
            });

        services.AddSingleton<HttpBasedLiveRouteProvider>();
        services.AddSingleton<PlaywrightLiveRouteProvider>();
        services.AddSingleton<ILiveRouteProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<HttpBasedLiveRouteProvider>());
        services.AddSingleton<ILiveRouteProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<PlaywrightLiveRouteProvider>());
        services.AddSingleton<ILiveRouteSnapshotCache, LiveRouteSnapshotCache>();
        services.AddSingleton<IRouteLiveService, RouteLiveService>();

        return services;
    }
}
