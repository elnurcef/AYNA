namespace Backend.Models;

public sealed class LiveRoutesOptions
{
    public const string SectionName = "LiveRoutes";

    public string ProviderMode { get; init; } = "auto";
    public string[] ProviderOrder { get; init; } = ["http", "playwright"];
    public int RefreshIntervalSeconds { get; init; } = 30;
    public int RouteDetailConcurrency { get; init; } = 8;
    public int HttpTimeoutSeconds { get; init; } = 60;
}
