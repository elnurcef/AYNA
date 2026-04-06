using System.Net.Http.Json;
using System.Text.Json;
using Backend.Helpers;
using Backend.Models;
using Microsoft.Extensions.Options;

namespace Backend.Services;

public sealed class HttpBasedLiveRouteProvider : ILiveRouteProvider
{
    public const string ClientName = "AynaLiveRoutes";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AynaLiveRouteOptions _aynaOptions;
    private readonly LiveRoutesOptions _liveRoutesOptions;
    private readonly ILogger<HttpBasedLiveRouteProvider> _logger;

    public HttpBasedLiveRouteProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AynaLiveRouteOptions> aynaOptions,
        IOptions<LiveRoutesOptions> liveRoutesOptions,
        ILogger<HttpBasedLiveRouteProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _aynaOptions = aynaOptions.Value;
        _liveRoutesOptions = liveRoutesOptions.Value;
        _logger = logger;
    }

    public string Name => "http";

    public async Task<LiveRouteSnapshot> GetLiveRoutesAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(ClientName);

        var busList = await client.GetFromJsonAsync<IReadOnlyList<AynaBusListItem>>(
            AynaLiveRouteUrlBuilder.BuildBusListUri(_aynaOptions),
            SerializerOptions,
            cancellationToken);

        if (busList is null || busList.Count == 0)
        {
            throw new LiveRouteProviderException("AYNA bus list request returned no routes.");
        }

        var details = await FetchRouteDetailsAsync(client, busList, cancellationToken);

        if (details.Count == 0)
        {
            throw new LiveRouteProviderException("AYNA route detail requests returned no usable route data.");
        }

        _logger.LogInformation("Loaded {Count} live routes from AYNA HTTP endpoints.", details.Count);

        return AynaLiveRouteMapper.MapSnapshot(details, Name);
    }

    private async Task<List<AynaBusDetail>> FetchRouteDetailsAsync(
        HttpClient client,
        IReadOnlyList<AynaBusListItem> busListItems,
        CancellationToken cancellationToken)
    {
        var concurrency = Math.Max(_liveRoutesOptions.RouteDetailConcurrency, 1);
        using var throttler = new SemaphoreSlim(concurrency);

        var detailTasks = busListItems.Select(async busListItem =>
        {
            await throttler.WaitAsync(cancellationToken);

            try
            {
                var detail = await client.GetFromJsonAsync<AynaBusDetail>(
                    AynaLiveRouteUrlBuilder.BuildBusByIdUri(_aynaOptions, busListItem.Id),
                    SerializerOptions,
                    cancellationToken);

                if (detail?.Routes is null || detail.Routes.Count == 0)
                {
                    return null;
                }

                return detail;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to load AYNA route detail for route {RouteId}.",
                    busListItem.Id);

                return null;
            }
            finally
            {
                throttler.Release();
            }
        });

        var details = await Task.WhenAll(detailTasks);

        return details
            .Where(detail => detail is not null)
            .Cast<AynaBusDetail>()
            .ToList();
    }
}
