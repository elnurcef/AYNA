namespace Backend.Models;

public sealed class BusAnalyticsQueryParameters
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? Route { get; init; }
    public string? OperatorName { get; init; }
    public DateOnly? Date { get; init; }
    public string? SortBy { get; init; } = "date";
    public string? SortDir { get; init; } = "desc";
}
