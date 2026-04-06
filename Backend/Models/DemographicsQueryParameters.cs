namespace Backend.Models;

public sealed class DemographicsQueryParameters
{
    public string Level { get; init; } = "meso";
    public string Metric { get; init; } = "population";

    public static bool IsSupportedLevel(string? value)
    {
        return Normalize(value) is "micro" or "meso" or "macro";
    }

    public static bool IsSupportedMetric(string? value)
    {
        return Normalize(value) is "population" or "jobs";
    }

    public string NormalizedLevel => Normalize(Level);

    public string NormalizedMetric => Normalize(Metric);

    private static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
