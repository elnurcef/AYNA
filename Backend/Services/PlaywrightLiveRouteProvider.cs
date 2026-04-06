using System.Text.Json;
using Backend.Helpers;
using Backend.Models;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Backend.Services;

public sealed class PlaywrightLiveRouteProvider : ILiveRouteProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AynaLiveRouteOptions _aynaOptions;
    private readonly LiveRoutesOptions _liveRoutesOptions;
    private readonly ILogger<PlaywrightLiveRouteProvider> _logger;

    public PlaywrightLiveRouteProvider(
        IOptions<AynaLiveRouteOptions> aynaOptions,
        IOptions<LiveRoutesOptions> liveRoutesOptions,
        ILogger<PlaywrightLiveRouteProvider> logger)
    {
        _aynaOptions = aynaOptions.Value;
        _liveRoutesOptions = liveRoutesOptions.Value;
        _logger = logger;
    }

    public string Name => "playwright";

    public async Task<LiveRouteSnapshot> GetLiveRoutesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync().WaitAsync(cancellationToken);
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _aynaOptions.Playwright.Headless
            }).WaitAsync(cancellationToken);

            await using var context = await browser.NewContextAsync().WaitAsync(cancellationToken);
            var page = await context.NewPageAsync().WaitAsync(cancellationToken);

            await page.GotoAsync(
                    AynaLiveRouteUrlBuilder.BuildSiteUri(_aynaOptions).AbsoluteUri,
                    new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle,
                        Timeout = _aynaOptions.Playwright.NavigationTimeoutMs
                    })
                .WaitAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(_aynaOptions.Playwright.AppReadySelector))
            {
                await page.WaitForSelectorAsync(
                        _aynaOptions.Playwright.AppReadySelector,
                        new PageWaitForSelectorOptions
                        {
                            Timeout = _aynaOptions.Playwright.NavigationTimeoutMs
                        })
                    .WaitAsync(cancellationToken);
            }

            var busList = await FetchJsonAsync<IReadOnlyList<AynaBusListItem>>(
                context.APIRequest,
                AynaLiveRouteUrlBuilder.BuildBusListUri(_aynaOptions).AbsoluteUri,
                cancellationToken);

            if (busList is null || busList.Count == 0)
            {
                throw new LiveRouteProviderException("AYNA browser-backed request returned no route list.");
            }

            var details = await FetchRouteDetailsAsync(context.APIRequest, busList, cancellationToken);

            if (details.Count == 0)
            {
                throw new LiveRouteProviderException("AYNA browser-backed request returned no usable route details.");
            }

            _logger.LogInformation("Loaded {Count} live routes through Playwright.", details.Count);

            return AynaLiveRouteMapper.MapSnapshot(details, Name);
        }
        catch (PlaywrightException exception)
        {
            throw new LiveRouteProviderException(
                "Playwright fallback failed. Make sure Chromium is installed for Microsoft.Playwright.",
                exception);
        }
    }

    private async Task<List<AynaBusDetail>> FetchRouteDetailsAsync(
        IAPIRequestContext apiRequestContext,
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
                var detail = await FetchJsonAsync<AynaBusDetail>(
                    apiRequestContext,
                    AynaLiveRouteUrlBuilder.BuildBusByIdUri(_aynaOptions, busListItem.Id).AbsoluteUri,
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
                    "Playwright fallback could not load route {RouteId}.",
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

    private static async Task<T?> FetchJsonAsync<T>(
        IAPIRequestContext apiRequestContext,
        string url,
        CancellationToken cancellationToken)
    {
        var response = await apiRequestContext.GetAsync(url).WaitAsync(cancellationToken);

        if (!response.Ok)
        {
            throw new LiveRouteProviderException(
                $"Playwright fallback request failed with HTTP {(int)response.Status}: {url}");
        }

        var payload = await response.TextAsync().WaitAsync(cancellationToken);

        return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }
}
