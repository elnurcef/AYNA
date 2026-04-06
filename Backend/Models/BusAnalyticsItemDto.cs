namespace Backend.Models;

public sealed record BusAnalyticsItemDto(
    DateOnly Date,
    int Hour,
    string Route,
    int TotalCount,
    int BySmartCard,
    int ByQr,
    int NumberOfBusses,
    string Operator);
