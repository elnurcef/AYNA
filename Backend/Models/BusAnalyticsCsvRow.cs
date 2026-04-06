namespace Backend.Models;

public sealed class BusAnalyticsCsvRow
{
    public DateTimeOffset Date { get; init; }
    public int Hour { get; init; }
    public string Route { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int BySmartCard { get; init; }
    public int ByQr { get; init; }
    public int NumberOfBusses { get; init; }
    public string Operator { get; init; } = string.Empty;
}
