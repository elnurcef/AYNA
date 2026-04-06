namespace Backend.Models;

public sealed class AynaLiveRouteOptions
{
    public const string SectionName = "LiveRoutes:Ayna";

    public string SiteUrl { get; init; } = "https://map.ayna.gov.az";
    public string ApiBaseUrl { get; init; } = "https://map-api.ayna.gov.az";
    public string BusListPath { get; init; } = "/api/bus/getBusList";
    public string BusByIdPath { get; init; } = "/api/bus/getBusById";
    public AynaPlaywrightOptions Playwright { get; init; } = new();
}

public sealed class AynaPlaywrightOptions
{
    public bool Headless { get; init; } = true;
    public int NavigationTimeoutMs { get; init; } = 30000;
    public string AppReadySelector { get; init; } = "#root";
}
